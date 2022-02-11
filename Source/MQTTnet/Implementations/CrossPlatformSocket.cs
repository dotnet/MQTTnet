// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Exceptions;

namespace MQTTnet.Implementations
{
    public sealed class CrossPlatformSocket : IDisposable
    {
        readonly Socket _socket;
        readonly Action _socketDisposeAction;

        NetworkStream _networkStream;

        public CrossPlatformSocket(AddressFamily addressFamily)
        {
            _socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socketDisposeAction = _socket.Dispose;
        }

        public CrossPlatformSocket()
        {
            // Having this constructor is important because avoiding the address family as parameter
            // will make use of dual mode in the .net framework.
            _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            _socketDisposeAction = _socket.Dispose;
        }

        public CrossPlatformSocket(Socket socket)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            _networkStream = new NetworkStream(socket, true);

            _socketDisposeAction = _socket.Dispose;
        }

        public bool NoDelay
        {
            // We cannot use the _NoDelay_ property from the socket because there is an issue in .NET 4.5.2, 4.6.
            // The decompiled code is: this.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.Debug, value ? 1 : 0);
            // Which is wrong because the "NoDelay" should be set and not "Debug".
            get => (int) _socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay) > 0;
            set => _socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, value ? 1 : 0);
        }

        public bool DualMode
        {
            get => _socket.DualMode;
            set => _socket.DualMode = value;
        }

        public int ReceiveBufferSize
        {
            get => _socket.ReceiveBufferSize;
            set => _socket.ReceiveBufferSize = value;
        }

        public int SendBufferSize
        {
            get => _socket.SendBufferSize;
            set => _socket.SendBufferSize = value;
        }

        public int SendTimeout
        {
            get => _socket.SendTimeout;
            set => _socket.SendTimeout = value;
        }

        public EndPoint RemoteEndPoint => _socket.RemoteEndPoint;

        public bool ReuseAddress
        {
            get => (int) _socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress) != 0;
            set => _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, value ? 1 : 0);
        }

        public async Task<CrossPlatformSocket> AcceptAsync()
        {
            try
            {
#if NET452 || NET461
                var clientSocket = await Task.Factory.FromAsync(_socket.BeginAccept, _socket.EndAccept, null).ConfigureAwait(false);
#else
                var clientSocket = await _socket.AcceptAsync().ConfigureAwait(false);
#endif
                return new CrossPlatformSocket(clientSocket);
            }
            catch (ObjectDisposedException)
            {
                // This will happen when _socket.EndAccept gets called by Task library but the socket is already disposed.
                return null;
            }
        }

        public void Bind(EndPoint localEndPoint)
        {
            if (localEndPoint is null) throw new ArgumentNullException(nameof(localEndPoint));

            _socket.Bind(localEndPoint);
        }

        public void Listen(int connectionBacklog)
        {
            _socket.Listen(connectionBacklog);
        }

        public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken)
        {
            if (host is null) throw new ArgumentNullException(nameof(host));

            try
            {
                _networkStream?.Dispose();

#if NET5_0_OR_GREATER
                await _socket.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);
#else
                // Workaround for: https://github.com/dotnet/corefx/issues/24430
                using (cancellationToken.Register(_socketDisposeAction))
                {
                    cancellationToken.ThrowIfCancellationRequested();

#if NET452 || NET461
                    await Task.Factory.FromAsync(_socket.BeginConnect, _socket.EndConnect, host, port, null).ConfigureAwait(false);
#else
                    await _socket.ConnectAsync(host, port).ConfigureAwait(false);
#endif
                }
#endif
                _networkStream = new NetworkStream(_socket, true);
            }
            catch (SocketException socketException)
            {
                if (socketException.SocketErrorCode == SocketError.OperationAborted)
                {
                    throw new OperationCanceledException();
                }

                throw new MqttCommunicationException($"Error while connecting with host '{host}:{port}'.", socketException);
            }
            catch (ObjectDisposedException)
            {
                // This will happen when _socket.EndConnect gets called by Task library but the socket is already disposed.
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        public async Task SendAsync(ArraySegment<byte> buffer, SocketFlags socketFlags)
        {
            try
            {
#if NET452 || NET461
                await Task.Factory.FromAsync(SocketWrapper.BeginSend, _socket.EndSend, new SocketWrapper(_socket, buffer, socketFlags)).ConfigureAwait(false);
#else
                await _socket.SendAsync(buffer, socketFlags).ConfigureAwait(false);
#endif
            }
            catch (ObjectDisposedException)
            {
                // This will happen when _socket.EndConnect gets called by Task library but the socket is already disposed.
            }
        }

        public async Task<int> ReceiveAsync(ArraySegment<byte> buffer, SocketFlags socketFlags)
        {
            try
            {
#if NET452 || NET461
                return await Task.Factory.FromAsync(SocketWrapper.BeginReceive, _socket.EndReceive, new SocketWrapper(_socket, buffer, socketFlags)).ConfigureAwait(false);
#else
                return await _socket.ReceiveAsync(buffer, socketFlags).ConfigureAwait(false);
#endif
            }
            catch (ObjectDisposedException)
            {
                // This will happen when _socket.EndReceive gets called by Task library but the socket is already disposed.
                return -1;
            }
        }

        public NetworkStream GetStream()
        {
            var networkStream = _networkStream;
            if (networkStream == null)
            {
                throw new IOException("The socket is not connected.");
            }

            return networkStream;
        }

        public void Dispose()
        {
            _networkStream?.Dispose();
            _socket?.Dispose();
        }

#if NET452 || NET461
        class SocketWrapper
        {
            readonly Socket _socket;
            readonly ArraySegment<byte> _buffer;
            readonly SocketFlags _socketFlags;

            public SocketWrapper(Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags)
            {
                _socket = socket;
                _buffer = buffer;
                _socketFlags = socketFlags;
            }

            public static IAsyncResult BeginSend(AsyncCallback callback, object state)
            {
                var socketWrapper = (SocketWrapper)state;
                return socketWrapper._socket.BeginSend(socketWrapper._buffer.Array, socketWrapper._buffer.Offset, socketWrapper._buffer.Count, socketWrapper._socketFlags, callback, state);
            }

            public static IAsyncResult BeginReceive(AsyncCallback callback, object state)
            {
                var socketWrapper = (SocketWrapper)state;
                return socketWrapper._socket.BeginReceive(socketWrapper._buffer.Array, socketWrapper._buffer.Offset, socketWrapper._buffer.Count, socketWrapper._socketFlags, callback, state);
            }
        }
#endif
    }
}