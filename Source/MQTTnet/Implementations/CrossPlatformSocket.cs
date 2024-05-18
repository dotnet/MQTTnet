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

#if !NET5_0_OR_GREATER
        readonly Action _socketDisposeAction;
#endif

        NetworkStream _networkStream;

        public CrossPlatformSocket(AddressFamily addressFamily, ProtocolType protocolType)
        {
            _socket = new Socket(addressFamily, SocketType.Stream, protocolType);

#if !NET5_0_OR_GREATER
            _socketDisposeAction = _socket.Dispose;
#endif
        }

        public CrossPlatformSocket(ProtocolType protocolType)
        {
            // Having this constructor is important because avoiding the address family as parameter
            // will make use of dual mode in the .net framework.
            _socket = new Socket(SocketType.Stream, protocolType);

#if !NET5_0_OR_GREATER
            _socketDisposeAction = _socket.Dispose;
#endif
        }

        CrossPlatformSocket(Socket socket)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            _networkStream = new NetworkStream(socket, true);

#if !NET5_0_OR_GREATER
            _socketDisposeAction = _socket.Dispose;
#endif
        }

        public bool DualMode
        {
            get => _socket.DualMode;
            set => _socket.DualMode = value;
        }

        public bool IsConnected => _socket.Connected;

        public bool KeepAlive
        {
            get => _socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive) as int? == 1;
            set => _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, value ? 1 : 0);
        }

        public int TcpKeepAliveInterval
        {
#if NETCOREAPP3_0_OR_GREATER
            get => _socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval) as int? ?? 0;
            set => _socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, value);
#else
            get { throw new NotSupportedException("TcpKeepAliveInterval requires at least netcoreapp3.0."); }
            set { throw new NotSupportedException("TcpKeepAliveInterval requires at least netcoreapp3.0."); }
#endif
        }

        public int TcpKeepAliveRetryCount
        {
#if NETCOREAPP3_0_OR_GREATER
            get => _socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount) as int? ?? 0;
            set => _socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, value);
#else
            get { throw new NotSupportedException("TcpKeepAliveRetryCount requires at least netcoreapp3.0."); }
            set { throw new NotSupportedException("TcpKeepAliveRetryCount requires at least netcoreapp3.0."); }
#endif
        }

        public int TcpKeepAliveTime
        {
#if NETCOREAPP3_0_OR_GREATER
            get => _socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime) as int? ?? 0;
            set => _socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, value);
#else
            get { throw new NotSupportedException("TcpKeepAliveTime requires at least netcoreapp3.0."); }
            set { throw new NotSupportedException("TcpKeepAliveTime requires at least netcoreapp3.0."); }
#endif
        }

        public LingerOption LingerState
        {
            get => _socket.LingerState;
            set => _socket.LingerState = value;
        }

        public EndPoint LocalEndPoint => _socket.LocalEndPoint;

        public bool NoDelay
        {
            // We cannot use the _NoDelay_ property from the socket because there is an issue in .NET 4.5.2, 4.6.
            // The decompiled code is: this.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.Debug, value ? 1 : 0);
            // Which is wrong because the "NoDelay" should be set and not "Debug".
            get => (int?)_socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay) != 0;
            set => _socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, value ? 1 : 0);
        }

        public int ReceiveBufferSize
        {
            get => _socket.ReceiveBufferSize;
            set => _socket.ReceiveBufferSize = value;
        }

        public EndPoint RemoteEndPoint => _socket.RemoteEndPoint;

        public bool ReuseAddress
        {
            get => _socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress) as int? != 0;
            set => _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, value ? 1 : 0);
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
                // This will happen when _socket.EndAccept_ gets called by Task library but the socket is already disposed.
                return null;
            }
        }

        public void Bind(EndPoint localEndPoint)
        {
            if (localEndPoint is null)
            {
                throw new ArgumentNullException(nameof(localEndPoint));
            }

            _socket.Bind(localEndPoint);
        }

        public Task ConnectAsync(string host, int port, CancellationToken cancellationToken)
        {
            return ConnectAsync(new DnsEndPoint(host, port), cancellationToken);
        }

        public async Task ConnectAsync(EndPoint endPoint, CancellationToken cancellationToken)
        {
            if (endPoint is null)
            {
                throw new ArgumentNullException(nameof(endPoint));
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
#if NETCOREAPP3_0_OR_GREATER
                if (_networkStream != null)
                {
                    await _networkStream.DisposeAsync().ConfigureAwait(false);
                }
#else
                _networkStream?.Dispose();
#endif

#if NET5_0_OR_GREATER
                await _socket.ConnectAsync(endPoint, cancellationToken).ConfigureAwait(false);
#else
                // Workaround for: https://github.com/dotnet/corefx/issues/24430
                using (cancellationToken.Register(_socketDisposeAction))
                {
#if NET452 || NET461
                    // This is a fix for Mono which behaves differently than dotnet.
                    // The connection will not be established when the DNS endpoint is used.
                    if (endPoint is DnsEndPoint dns && dns.AddressFamily == AddressFamily.Unspecified)
                    {
                        await Task.Factory.FromAsync(_socket.BeginConnect, _socket.EndConnect, dns.Host, dns.Port, null).ConfigureAwait(false);
                    }
                    else
                    {
                        await Task.Factory.FromAsync(_socket.BeginConnect, _socket.EndConnect, endPoint, null).ConfigureAwait(false);
                    }
#else

                    // This is a fix for Mono which behaves differently than dotnet.
                    // The connection will not be established when the DNS endpoint is used.
                    if (endPoint is DnsEndPoint dns && dns.AddressFamily == AddressFamily.Unspecified)
                    {
                        await _socket.ConnectAsync(dns.Host, dns.Port).ConfigureAwait(false);
                    }
                    else
                    {
                        await _socket.ConnectAsync(endPoint).ConfigureAwait(false);
                    }
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

                if (socketException.SocketErrorCode == SocketError.TimedOut)
                {
                    throw new MqttCommunicationTimedOutException();
                }

                throw new MqttCommunicationException($"Error while connecting host '{endPoint}'.", socketException);
            }
            catch (ObjectDisposedException)
            {
                // This will happen when _socket.EndConnect_ gets called by Task library but the socket is already disposed.
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        public void Dispose()
        {
            _networkStream?.Dispose();
            _socket?.Dispose();
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

        public void Listen(int connectionBacklog)
        {
            _socket.Listen(connectionBacklog);
        }

#if NET452 || NET461
        public async Task<int> ReceiveAsync(ArraySegment<byte> buffer, SocketFlags socketFlags)
        {
            try
            {
                return await Task.Factory.FromAsync(SocketWrapper.BeginReceive, _socket.EndReceive, new SocketWrapper(_socket, buffer, socketFlags)).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                // This will happen when _socket.EndReceive_ gets called by Task library but the socket is already disposed.
                return -1;
            }
        }
#else
        public Task<int> ReceiveAsync(ArraySegment<byte> buffer, SocketFlags socketFlags)
        {
            return _socket.ReceiveAsync(buffer, socketFlags);
        }
#endif

#if NET452 || NET461
        public async Task SendAsync(ArraySegment<byte> buffer, SocketFlags socketFlags)
        {
            try
            {
                await Task.Factory.FromAsync(SocketWrapper.BeginSend, _socket.EndSend, new SocketWrapper(_socket, buffer, socketFlags)).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                // This will happen when _socket.EndSend_ gets called by Task library but the socket is already disposed.
            }
        }
#else
        public Task SendAsync(ArraySegment<byte> buffer, SocketFlags socketFlags)
        {
            return _socket.SendAsync(buffer, socketFlags);
        }
#endif

#if NET452 || NET461
        sealed class SocketWrapper
        {
            readonly ArraySegment<byte> _buffer;
            readonly Socket _socket;
            readonly SocketFlags _socketFlags;

            public SocketWrapper(Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags)
            {
                _socket = socket;
                _buffer = buffer;
                _socketFlags = socketFlags;
            }

            public static IAsyncResult BeginReceive(AsyncCallback callback, object state)
            {
                var socketWrapper = (SocketWrapper)state;
                return socketWrapper._socket.BeginReceive(
                    socketWrapper._buffer.Array,
                    socketWrapper._buffer.Offset,
                    socketWrapper._buffer.Count,
                    socketWrapper._socketFlags,
                    callback,
                    state);
            }

            public static IAsyncResult BeginSend(AsyncCallback callback, object state)
            {
                var socketWrapper = (SocketWrapper)state;
                return socketWrapper._socket.BeginSend(
                    socketWrapper._buffer.Array,
                    socketWrapper._buffer.Offset,
                    socketWrapper._buffer.Count,
                    socketWrapper._socketFlags,
                    callback,
                    state);
            }
        }
#endif
    }
}