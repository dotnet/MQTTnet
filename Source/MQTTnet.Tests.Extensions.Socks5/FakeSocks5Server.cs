// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Tests.Extensions.Socks5;

/// <summary>
///     Scriptable in-process SOCKS5 server used by the test suite.
///     Listens on 127.0.0.1 on a random port and runs a single, configurable handshake per
///     accepted connection. On a successful CONNECT, it opens a TCP connection to the destination
///     described by the CONNECT request and bidirectionally relays bytes between the client and
///     the destination, allowing tests to point the SOCKS5 client at the in-memory MQTTnet
///     broker.
/// </summary>
internal sealed class FakeSocks5Server : IAsyncDisposable
{
    readonly TcpListener _listener;
    readonly CancellationTokenSource _cts = new();
    readonly Task _acceptLoop;
    readonly List<Task> _connectionTasks = new();
    readonly object _connectionsLock = new();
    readonly List<RecordedConnect> _recordedConnects = new();
    readonly object _recordedLock = new();

    public FakeSocks5Server(FakeSocks5ServerOptions options)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));

        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();

        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;

        _acceptLoop = Task.Run(AcceptLoopAsync);
    }

    public int Port { get; }

    public FakeSocks5ServerOptions Options { get; }

    public IReadOnlyList<RecordedConnect> RecordedConnects
    {
        get
        {
            lock (_recordedLock)
            {
                return _recordedConnects.ToArray();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            _cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        try
        {
            _listener.Stop();
        }
        catch (Exception)
        {
            // ignore
        }

        try
        {
            await _acceptLoop.ConfigureAwait(false);
        }
        catch (Exception)
        {
            // ignore
        }

        Task[] pending;
        lock (_connectionsLock)
        {
            pending = _connectionTasks.ToArray();
        }

        try
        {
            await Task.WhenAll(pending).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // ignore
        }

        _cts.Dispose();
    }

    async Task AcceptLoopAsync()
    {
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await _listener.AcceptTcpClientAsync(_cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (SocketException)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }

                var task = Task.Run(() => HandleClientAsync(client));
                lock (_connectionsLock)
                {
                    _connectionTasks.Add(task);
                }
            }
        }
        catch (Exception)
        {
            // swallow — diagnostic-only
        }
    }

    async Task HandleClientAsync(TcpClient client)
    {
        using (client)
        using (var clientStream = client.GetStream())
        {
            try
            {
                if (Options.DropAfterAccept)
                {
                    return;
                }

                // Greeting: VER NMETHODS METHODS[]
                var versionAndCount = new byte[2];
                await ReadExactlyAsync(clientStream, versionAndCount, _cts.Token).ConfigureAwait(false);
                if (versionAndCount[0] != 0x05)
                {
                    return;
                }

                var methods = new byte[versionAndCount[1]];
                await ReadExactlyAsync(clientStream, methods, _cts.Token).ConfigureAwait(false);

                if (Options.GreetingResponseOverride != null)
                {
                    await clientStream.WriteAsync(Options.GreetingResponseOverride, _cts.Token).ConfigureAwait(false);
                    if (Options.DropAfterGreetingResponse)
                    {
                        return;
                    }
                }
                else
                {
                    byte method = SelectMethod(methods);
                    await clientStream.WriteAsync(new byte[] { 0x05, method }, _cts.Token).ConfigureAwait(false);

                    if (method == 0xFF)
                    {
                        return;
                    }

                    if (method == 0x02)
                    {
                        if (!await HandleAuthAsync(clientStream).ConfigureAwait(false))
                        {
                            return;
                        }
                    }
                }

                // CONNECT request: VER CMD RSV ATYP DST.ADDR DST.PORT
                var header = new byte[4];
                await ReadExactlyAsync(clientStream, header, _cts.Token).ConfigureAwait(false);

                if (header[0] != 0x05)
                {
                    // RFC 1928 §4: VER must be X'05' in every message.
                    return;
                }

                if (header[1] != 0x01)
                {
                    // RFC 1928 §4: only CONNECT (0x01) is supported by this fake server. BIND (0x02)
                    // and UDP ASSOCIATE (0x03) are signaled as unsupported per spec via REP=0x07.
                    await WriteReplyAsync(clientStream, 0x07, Options.ReplyReservedByte, Options.ReplyAddressType, Options.ReplyBindHost, Options.ReplyBindPort).ConfigureAwait(false);
                    return;
                }

                string targetHost;
                IPAddress targetIp = null;
                byte atyp = header[3];
                switch (atyp)
                {
                    case 0x01:
                    {
                        var addr = new byte[4];
                        await ReadExactlyAsync(clientStream, addr, _cts.Token).ConfigureAwait(false);
                        targetIp = new IPAddress(addr);
                        targetHost = targetIp.ToString();
                        break;
                    }

                    case 0x03:
                    {
                        var lenBuf = new byte[1];
                        await ReadExactlyAsync(clientStream, lenBuf, _cts.Token).ConfigureAwait(false);
                        var hostBytes = new byte[lenBuf[0]];
                        await ReadExactlyAsync(clientStream, hostBytes, _cts.Token).ConfigureAwait(false);
                        targetHost = Encoding.UTF8.GetString(hostBytes);
                        break;
                    }

                    case 0x04:
                    {
                        var addr = new byte[16];
                        await ReadExactlyAsync(clientStream, addr, _cts.Token).ConfigureAwait(false);
                        targetIp = new IPAddress(addr);
                        targetHost = targetIp.ToString();
                        break;
                    }

                    default:
                        await WriteReplyAsync(clientStream, 0x08, Options.ReplyReservedByte, Options.ReplyAddressType, Options.ReplyBindHost, Options.ReplyBindPort).ConfigureAwait(false);
                        return;
                }

                var portBuf = new byte[2];
                await ReadExactlyAsync(clientStream, portBuf, _cts.Token).ConfigureAwait(false);
                var targetPort = BinaryPrimitives.ReadUInt16BigEndian(portBuf);

                lock (_recordedLock)
                {
                    _recordedConnects.Add(new RecordedConnect(atyp, targetHost, targetPort));
                }

                if (Options.ConnectReplyCode != 0x00)
                {
                    await WriteReplyAsync(clientStream, Options.ConnectReplyCode, Options.ReplyReservedByte, Options.ReplyAddressType, Options.ReplyBindHost, Options.ReplyBindPort).ConfigureAwait(false);
                    return;
                }

                // Open the destination connection and start relaying.
                TcpClient upstream;
                try
                {
                    upstream = new TcpClient();
                    if (targetIp != null)
                    {
                        await upstream.ConnectAsync(targetIp, targetPort, _cts.Token).ConfigureAwait(false);
                    }
                    else
                    {
                        await upstream.ConnectAsync(targetHost, targetPort, _cts.Token).ConfigureAwait(false);
                    }
                }
                catch (Exception)
                {
                    await WriteReplyAsync(clientStream, 0x04, Options.ReplyReservedByte, Options.ReplyAddressType, Options.ReplyBindHost, Options.ReplyBindPort).ConfigureAwait(false);
                    return;
                }

                using (upstream)
                using (var upstreamStream = upstream.GetStream())
                {
                    await WriteReplyAsync(clientStream, 0x00, Options.ReplyReservedByte, Options.ReplyAddressType, Options.ReplyBindHost, Options.ReplyBindPort).ConfigureAwait(false);

                    var c2u = RelayAsync(clientStream, upstreamStream, _cts.Token);
                    var u2c = RelayAsync(upstreamStream, clientStream, _cts.Token);
                    await Task.WhenAny(c2u, u2c).ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                // swallow — diagnostic-only
            }
        }
    }

    byte SelectMethod(byte[] offered)
    {
        var allowedMethods = Options.AllowedMethods;
        foreach (var m in offered)
        {
            foreach (var allowed in allowedMethods)
            {
                if (m == allowed)
                {
                    return m;
                }
            }
        }

        return 0xFF;
    }

    async Task<bool> HandleAuthAsync(NetworkStream stream)
    {
        var header = new byte[2];
        await ReadExactlyAsync(stream, header, _cts.Token).ConfigureAwait(false);
        if (header[0] != 0x01)
        {
            return false;
        }

        var ulen = header[1];
        var userBytes = new byte[ulen];
        await ReadExactlyAsync(stream, userBytes, _cts.Token).ConfigureAwait(false);

        var plenBuf = new byte[1];
        await ReadExactlyAsync(stream, plenBuf, _cts.Token).ConfigureAwait(false);
        var passBytes = new byte[plenBuf[0]];
        await ReadExactlyAsync(stream, passBytes, _cts.Token).ConfigureAwait(false);

        var user = Encoding.UTF8.GetString(userBytes);
        var pass = Encoding.UTF8.GetString(passBytes);

        var ok = Options.ExpectedUsername == user && Options.ExpectedPassword == pass;
        if (Options.ForceAuthFailure)
        {
            ok = false;
        }

        await stream.WriteAsync(new byte[] { 0x01, ok ? (byte)0x00 : (byte)0x01 }, _cts.Token).ConfigureAwait(false);
        return ok;
    }

    static async Task WriteReplyAsync(NetworkStream stream, byte rep, byte replyReserved, byte replyAtyp, string replyHost, ushort replyPort)
    {
        // VER REP RSV ATYP BND.ADDR BND.PORT
        //
        // RFC 1928 §6 says BND.ADDR contains the "associated IP address" (i.e. the server-side
        // endpoint it used to reach the destination). Most real SOCKS5 servers reply with
        // ATYP=0x01 (IPv4) + a zeroed address when they have nothing meaningful to report,
        // which is the safe default we use here. Tests can override the reply ATYP via
        // FakeSocks5ServerOptions.ReplyAddressType to exercise the client's reply parser.

        switch (replyAtyp)
        {
            case 0x04:
            {
                var reply = new byte[4 + 16 + 2];
                reply[0] = 0x05;
                reply[1] = rep;
                reply[2] = replyReserved;
                reply[3] = 0x04;
                if (!string.IsNullOrEmpty(replyHost) && IPAddress.TryParse(replyHost, out var ip) && ip.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    var bytes = ip.GetAddressBytes();
                    Buffer.BlockCopy(bytes, 0, reply, 4, 16);
                }
                BinaryPrimitives.WriteUInt16BigEndian(reply.AsSpan(20, 2), replyPort);
                await stream.WriteAsync(reply).ConfigureAwait(false);
                return;
            }

            case 0x03:
            {
                var host = string.IsNullOrEmpty(replyHost) ? "0.0.0.0" : replyHost;
                var hostBytes = Encoding.UTF8.GetBytes(host);
                if (hostBytes.Length > 255)
                {
                    Array.Resize(ref hostBytes, 255);
                }

                var reply = new byte[4 + 1 + hostBytes.Length + 2];
                reply[0] = 0x05;
                reply[1] = rep;
                reply[2] = replyReserved;
                reply[3] = 0x03;
                reply[4] = (byte)hostBytes.Length;
                Buffer.BlockCopy(hostBytes, 0, reply, 5, hostBytes.Length);
                BinaryPrimitives.WriteUInt16BigEndian(reply.AsSpan(5 + hostBytes.Length, 2), replyPort);
                await stream.WriteAsync(reply).ConfigureAwait(false);
                return;
            }

            default:
            {
                var reply = new byte[4 + 4 + 2];
                reply[0] = 0x05;
                reply[1] = rep;
                reply[2] = replyReserved;
                reply[3] = 0x01;
                if (!string.IsNullOrEmpty(replyHost) && IPAddress.TryParse(replyHost, out var ip) && ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    var bytes = ip.GetAddressBytes();
                    Buffer.BlockCopy(bytes, 0, reply, 4, 4);
                }
                BinaryPrimitives.WriteUInt16BigEndian(reply.AsSpan(8, 2), replyPort);
                await stream.WriteAsync(reply).ConfigureAwait(false);
                return;
            }
        }
    }

    static async Task RelayAsync(NetworkStream source, NetworkStream destination, CancellationToken cancellationToken)
    {
        var buffer = new byte[16 * 1024];
        try
        {
            while (true)
            {
                var read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (read <= 0)
                {
                    return;
                }

                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception)
        {
            // ignore
        }
    }

    static async Task ReadExactlyAsync(NetworkStream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), cancellationToken).ConfigureAwait(false);
            if (read <= 0)
            {
                throw new EndOfStreamException();
            }

            offset += read;
        }
    }

    public sealed record RecordedConnect(byte AddressType, string Host, int Port);
}

internal sealed class FakeSocks5ServerOptions
{
    /// <summary>
    ///     The SOCKS5 auth methods the server will accept, in preference order matched against the
    ///     client's offered methods. Defaults to no-auth only.
    /// </summary>
    public byte[] AllowedMethods { get; set; } = [0x00];

    public string ExpectedUsername { get; set; }

    public string ExpectedPassword { get; set; }

    /// <summary>
    ///     Forces the username/password sub-negotiation to return STATUS=0x01 regardless of the
    ///     supplied credentials. Useful for tests that exercise the auth-failure path.
    /// </summary>
    public bool ForceAuthFailure { get; set; }

    /// <summary>
    ///     SOCKS5 reply code for the CONNECT response. <c>0x00</c> means "succeeded" and the
    ///     server will additionally bridge bytes to the destination.
    /// </summary>
    public byte ConnectReplyCode { get; set; }

    /// <summary>
    ///     Reserved byte included in the CONNECT reply. Defaults to <c>0x00</c> per RFC 1928.
    /// </summary>
    public byte ReplyReservedByte { get; set; }

    /// <summary>
    ///     When set, the server sends this byte sequence instead of the regular greeting response.
    ///     Used to simulate malformed responses (e.g. wrong version).
    /// </summary>
    public byte[] GreetingResponseOverride { get; set; }

    /// <summary>
    ///     If <c>true</c>, closes the connection right after writing the greeting response
    ///     override. Used together with <see cref="GreetingResponseOverride"/> for protocol-error
    ///     tests.
    /// </summary>
    public bool DropAfterGreetingResponse { get; set; }

    /// <summary>
    ///     If <c>true</c>, the server closes the connection immediately after accepting (before
    ///     reading the greeting). Used to simulate proxy mid-handshake disconnects.
    /// </summary>
    public bool DropAfterAccept { get; set; }

    /// <summary>
    ///     Address type used when constructing CONNECT replies. Defaults to <c>0x01</c> (IPv4),
    ///     which matches the behavior of real SOCKS5 servers — they always reply with a resolved
    ///     address, regardless of how the client expressed the destination. Override to
    ///     <c>0x03</c> or <c>0x04</c> to exercise the client's reply parser.
    /// </summary>
    public byte ReplyAddressType { get; set; } = 0x01;

    /// <summary>
    ///     Bound host included in the CONNECT reply (BND.ADDR). Format depends on
    ///     <see cref="ReplyAddressType"/>: IPv4/IPv6 textual representation for ATYP 0x01/0x04,
    ///     a domain name for ATYP 0x03. When <c>null</c> or empty the server writes a zeroed
    ///     address (0.0.0.0, ::, or "0.0.0.0" respectively).
    /// </summary>
    public string ReplyBindHost { get; set; }

    /// <summary>
    ///     Bound port included in the CONNECT reply (BND.PORT). Defaults to <c>0</c>, which
    ///     is what real SOCKS5 servers return when no meaningful port is available.
    /// </summary>
    public ushort ReplyBindPort { get; set; }
}
