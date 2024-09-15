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

namespace MQTTnet.Implementations;

public sealed class CrossPlatformSocket : IDisposable
{
    readonly Socket _socket;

    NetworkStream _networkStream;

    public CrossPlatformSocket(AddressFamily addressFamily, ProtocolType protocolType)
    {
        _socket = new Socket(addressFamily, SocketType.Stream, protocolType);
    }

    public CrossPlatformSocket(ProtocolType protocolType)
    {
        // Having this constructor is important because avoiding the address family as parameter
        // will make use of dual mode in the .net framework.
        _socket = new Socket(SocketType.Stream, protocolType);
    }

    CrossPlatformSocket(Socket socket)
    {
        _socket = socket ?? throw new ArgumentNullException(nameof(socket));
        _networkStream = new NetworkStream(socket, true);
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

    public LingerOption LingerState
    {
        get => _socket.LingerState;
        set => _socket.LingerState = value;
    }

    public EndPoint LocalEndPoint => _socket.LocalEndPoint;

    public bool NoDelay
    {
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

    public int TcpKeepAliveInterval
    {
        get => _socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval) as int? ?? 0;
        set => _socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, value);
    }

    public int TcpKeepAliveRetryCount
    {
        get => _socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount) as int? ?? 0;
        set => _socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, value);
    }

    public int TcpKeepAliveTime
    {
        get => _socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime) as int? ?? 0;
        set => _socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, value);
    }

    public async Task<CrossPlatformSocket> AcceptAsync(CancellationToken cancellationToken)
    {
        try
        {
            var clientSocket = await _socket.AcceptAsync(cancellationToken).ConfigureAwait(false);
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

    public async Task ConnectAsync(EndPoint endPoint, CancellationToken cancellationToken)
    {
        if (endPoint is null)
        {
            throw new ArgumentNullException(nameof(endPoint));
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            if (_networkStream != null)
            {
                await _networkStream.DisposeAsync().ConfigureAwait(false);
            }

            await _socket.ConnectAsync(endPoint, cancellationToken).ConfigureAwait(false);
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

    public Task<int> ReceiveAsync(ArraySegment<byte> buffer, SocketFlags socketFlags)
    {
        return _socket.ReceiveAsync(buffer, socketFlags);
    }

    public Task SendAsync(ArraySegment<byte> buffer, SocketFlags socketFlags)
    {
        return _socket.SendAsync(buffer, socketFlags);
    }
}