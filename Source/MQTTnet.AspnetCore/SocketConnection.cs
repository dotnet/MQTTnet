// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using MQTTnet.Exceptions;

namespace MQTTnet.AspNetCore;

public sealed class SocketConnection : ConnectionContext
{
    readonly EndPoint _endPoint;
    volatile bool _aborted;
    IDuplexPipe _application;
    SocketReceiver _receiver;
    SocketSender _sender;

    Socket _socket;

    public SocketConnection(EndPoint endPoint)
    {
        _endPoint = endPoint;
    }

    public SocketConnection(Socket socket)
    {
        _socket = socket;
        _endPoint = socket.RemoteEndPoint;

        _sender = new SocketSender(_socket, PipeScheduler.ThreadPool);
        _receiver = new SocketReceiver(_socket, PipeScheduler.ThreadPool);
    }

    public override string ConnectionId { get; set; }
    public override IFeatureCollection Features { get; }

    public bool IsConnected { get; private set; }
    public override IDictionary<object, object> Items { get; set; }
    public override IDuplexPipe Transport { get; set; }

    public override ValueTask DisposeAsync()
    {
        IsConnected = false;

        Transport?.Output.Complete();
        Transport?.Input.Complete();

        _socket?.Dispose();

        return base.DisposeAsync();
    }

    public async Task StartAsync()
    {
        if (_socket == null)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _sender = new SocketSender(_socket, PipeScheduler.ThreadPool);
            _receiver = new SocketReceiver(_socket, PipeScheduler.ThreadPool);
            await _socket.ConnectAsync(_endPoint);
        }

        var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);

        Transport = pair.Transport;
        _application = pair.Application;

        _ = ExecuteAsync();

        IsConnected = true;
    }

    Exception ConnectionAborted()
    {
        return new MqttCommunicationException("Connection Aborted");
    }

    async Task DoReceive()
    {
        Exception error = null;

        try
        {
            await ProcessReceives();
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionReset)
        {
            error = new MqttCommunicationException(ex);
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted || ex.SocketErrorCode == SocketError.ConnectionAborted ||
                                         ex.SocketErrorCode == SocketError.Interrupted || ex.SocketErrorCode == SocketError.InvalidArgument)
        {
            if (!_aborted)
            {
                // Calling Dispose after ReceiveAsync can cause an "InvalidArgument" error on *nix.
                error = ConnectionAborted();
            }
        }
        catch (ObjectDisposedException)
        {
            if (!_aborted)
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
            if (_aborted)
            {
                error = error ?? ConnectionAborted();
            }

            _application.Output.Complete(error);
        }
    }

    async Task<Exception> DoSend()
    {
        Exception error = null;

        try
        {
            await ProcessSends();
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted)
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
            _aborted = true;
            _socket.Shutdown(SocketShutdown.Both);
        }

        return error;
    }

    async Task ExecuteAsync()
    {
        Exception sendError = null;
        try
        {
            // Spawn send and receive logic
            var receiveTask = DoReceive();
            var sendTask = DoSend();

            // If the sending task completes then close the receive
            // We don't need to do this in the other direction because the kestrel
            // will trigger the output closing once the input is complete.
            if (await Task.WhenAny(receiveTask, sendTask).ConfigureAwait(false) == sendTask)
            {
                // Tell the reader it's being aborted
                _socket.Dispose();
            }

            // Now wait for both to complete
            await receiveTask;
            sendError = await sendTask;

            // Dispose the socket(should noop if already called)
            _socket.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected exception in {nameof(SocketConnection)}.{nameof(StartAsync)}: " + ex);
        }
        finally
        {
            // Complete the output after disposing the socket
            await _application.Input.CompleteAsync(sendError).ConfigureAwait(false);
        }
    }

    async Task ProcessReceives()
    {
        while (true)
        {
            // Ensure we have some reasonable amount of buffer space
            var buffer = _application.Output.GetMemory();

            var bytesReceived = await _receiver.ReceiveAsync(buffer);

            if (bytesReceived == 0)
            {
                // FIN
                break;
            }

            _application.Output.Advance(bytesReceived);

            var flushTask = _application.Output.FlushAsync();

            if (!flushTask.IsCompleted)
            {
                await flushTask;
            }

            var result = flushTask.GetAwaiter().GetResult();
            if (result.IsCompleted)
            {
                // Pipe consumer is shut down, do we stop writing
                break;
            }
        }
    }

    async Task ProcessSends()
    {
        while (true)
        {
            // Wait for data to write from the pipe producer
            var result = await _application.Input.ReadAsync();
            var buffer = result.Buffer;

            if (result.IsCanceled)
            {
                break;
            }

            var end = buffer.End;
            var isCompleted = result.IsCompleted;
            if (!buffer.IsEmpty)
            {
                await _sender.SendAsync(buffer);
            }

            _application.Input.AdvanceTo(end);

            if (isCompleted)
            {
                break;
            }
        }
    }
}