// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using MQTTnet.AspNetCore.Client.Tcp;
using MQTTnet.Exceptions;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal
{
    public sealed class SocketConnection : ConnectionContext, IDisposable
    {
        private static readonly int MinAllocBufferSize = 2048;
        private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private static readonly bool IsMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        private readonly EndPoint _endPoint;
        private readonly CancellationTokenSource _connectionClosedTokenSource = new CancellationTokenSource();
        private readonly object _shutdownLock = new object();
        
        private Socket _socket;
        private SocketReceiver _receiver;
        private SocketSender _sender;
        private IDuplexPipe _application;
        
        private volatile bool _socketDisposed;
        private volatile Exception _shutdownReason;

        public bool IsConnected { get; private set; }
        public override string ConnectionId { get; set; }
        public override IFeatureCollection Features { get; }
        public override IDictionary<object, object> Items { get; set; }
        public override IDuplexPipe Transport { get; set; }

        public SocketConnection(EndPoint endPoint)
        {
            _endPoint = endPoint;
        }

        public SocketConnection(Socket socket)
        {
            Debug.Assert(socket != null);

            _socket = socket;
            _endPoint = socket.RemoteEndPoint;

            // On *nix platforms, Sockets already dispatches to the ThreadPool.
            // Yes, the IOQueues are still used for the PipeSchedulers. This is intentional.
            // https://github.com/aspnet/KestrelHttpServer/issues/2573
            var awaiterScheduler = IsWindows ? PipeScheduler.ThreadPool : PipeScheduler.Inline;

            _receiver = new SocketReceiver(_socket, awaiterScheduler);
            _sender = new SocketSender(_socket, awaiterScheduler);
        }
        
        public async Task StartAsync()
        {
            if (_socket == null)
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _sender = new SocketSender(_socket, PipeScheduler.ThreadPool);
                _receiver = new SocketReceiver(_socket, PipeScheduler.ThreadPool);
                await _socket.ConnectAsync(_endPoint).ConfigureAwait(false);
            }

            var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);

            Transport = pair.Transport;
            _application = pair.Application;
            
            _ = ExecuteAsync();

            IsConnected = true;
        }

        private async Task ExecuteAsync()
        {
            // Spawn send and receive logic
            var receiveTask = DoReceive();
            var sendTask = DoSend();

            // Now wait for both to complete
            await receiveTask;
            await sendTask;

            _receiver.Dispose();
            _sender.Dispose();
            ThreadPool.UnsafeQueueUserWorkItem(state => ((SocketConnection)state).CancelConnectionClosedToken(), this);
        }

        private async Task DoReceive()
        {
            Exception error = null;

            try
            {
                await ProcessReceives().ConfigureAwait(false);
            }

            catch (SocketException ex) when (IsConnectionResetError(ex.SocketErrorCode))
            {
                error = new MqttCommunicationException(ex);
            }
            catch (SocketException ex) when (IsConnectionAbortError(ex.SocketErrorCode))
            {
                if (!_socketDisposed)
                {
                    // Calling Dispose after ReceiveAsync can cause an "InvalidArgument" error on *nix.
                    error = ConnectionAborted();
                }
            }
            catch (ObjectDisposedException)
            {
                if (!_socketDisposed)
                {
                    error = ConnectionAborted();
                }
            }
            catch (IOException ex)
            {
                error = ex;
            }
            catch (Exception ex)
            {
                error = new IOException(ex.Message, ex);
            }
            finally
            {
                if (_socketDisposed)
                {
                    error = error ?? ConnectionAborted();
                }

                // If Shutdown() has already bee called, assume that was the reason ProcessReceives() exited.
                _application.Output.Complete(_shutdownReason ?? error);
            }
        }

        private async Task ProcessReceives()
        {
            var input = _application.Output;

            while (true)
            {
                // Wait for data before allocating a buffer.
                await _receiver.WaitForDataAsync();

                // Ensure we have some reasonable amount of buffer space
                var buffer = input.GetMemory(MinAllocBufferSize);

                var bytesReceived = await _receiver.ReceiveAsync(buffer);

                if (bytesReceived == 0)
                {
                    // FIN
                    break;
                }

                input.Advance(bytesReceived);

                var flushTask = input.FlushAsync();

                FlushResult result;
                if (!flushTask.IsCompleted)
                {
                    result = await flushTask.ConfigureAwait(false);
                }
                else 
                {
                    result = flushTask.Result;
                }

                if (result.IsCompleted || result.IsCanceled)
                {
                    // Pipe consumer is shut down, do we stop writing
                    break;
                }
            }
        }

        public override void Abort(ConnectionAbortedException abortReason)
        {
            // Try to gracefully close the socket to match libuv behavior.
            Shutdown(abortReason);

            // Cancel ProcessSends loop after calling shutdown to ensure the correct _shutdownReason gets set.
            _application.Input.CancelPendingRead();
        }

        // Only called after connection middleware is complete which means the ConnectionClosed token has fired.
        public void Dispose()
        {
            _connectionClosedTokenSource.Dispose();
        }


        private Exception ConnectionAborted()
        {
            return new MqttCommunicationException("Connection Aborted");
        }

        private async Task DoSend()
        {
            Exception error = null;

            try
            {
                await ProcessSends().ConfigureAwait(false);
            }
            catch (SocketException ex) when (IsConnectionAbortError(ex.SocketErrorCode))
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (IOException ex)
            {
                error = ex;
            }
            catch (Exception ex)
            {
                error = new IOException(ex.Message, ex);
            }
            finally
            {
                Shutdown(error);

                // Complete the output after disposing the socket
                _application.Input.Complete(error);

                // Cancel any pending flushes so that the input loop is un-paused
                _application.Output.CancelPendingFlush();
            }
        }

        private async Task ProcessSends()
        {
            var output = _application.Input;

            while (true)
            {
                var result = await output.ReadAsync().ConfigureAwait(false);

                if (result.IsCanceled)
                {
                    break;
                }

                var buffer = result.Buffer;

                var end = buffer.End;
                var isCompleted = result.IsCompleted;
                if (!buffer.IsEmpty)
                {
                    await _sender.SendAsync(buffer);
                }

                output.AdvanceTo(end);

                if (isCompleted)
                {
                    break;
                }
            }
        }

        private void Shutdown(Exception shutdownReason)
        {
            lock (_shutdownLock)
            {
                if (_socketDisposed)
                {
                    return;
                }

                // Make sure to close the connection only after the _aborted flag is set.
                // Without this, the RequestsCanBeAbortedMidRead test will sometimes fail when
                // a BadHttpRequestException is thrown instead of a TaskCanceledException.
                _socketDisposed = true;

                // shutdownReason should only be null if the output was completed gracefully, so no one should ever
                // ever observe the nondescript ConnectionAbortedException except for connection middleware attempting
                // to half close the connection which is currently unsupported.
                _shutdownReason = shutdownReason ?? new ConnectionAbortedException("The Socket transport's send loop completed gracefully.");

                try
                {
                    // Try to gracefully close the socket even for aborts to match libuv behavior.
                    _socket.Shutdown(SocketShutdown.Both);
                }
                catch
                {
                    // Ignore any errors from Socket.Shutdown() since we're tearing down the connection anyway.
                }

                _socket.Dispose();
            }
        }

        private void CancelConnectionClosedToken()
        {
            try
            {
                _connectionClosedTokenSource.Cancel();
            }
            catch (Exception)
            {
            }
        }

        private static bool IsConnectionResetError(SocketError errorCode)
        {
            // A connection reset can be reported as SocketError.ConnectionAborted on Windows.
            // ProtocolType can be removed once https://github.com/dotnet/corefx/issues/31927 is fixed.
            return errorCode == SocketError.ConnectionReset ||
                   errorCode == SocketError.Shutdown ||
                   (errorCode == SocketError.ConnectionAborted && IsWindows) ||
                   (errorCode == SocketError.ProtocolType && IsMacOS);
        }

        private static bool IsConnectionAbortError(SocketError errorCode)
        {
            // Calling Dispose after ReceiveAsync can cause an "InvalidArgument" error on *nix.
            return errorCode == SocketError.OperationAborted ||
                   errorCode == SocketError.Interrupted ||
                   (errorCode == SocketError.InvalidArgument && !IsWindows);
        }
    }
}