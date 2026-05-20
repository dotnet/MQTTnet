// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using MQTTnet.Channel;
using MQTTnet.Exceptions;

namespace MQTTnet.Extensions.Socks5;

/// <summary>
///     <see cref="IMqttClientStreamProvider"/> implementation that tunnels the connection through
///     a SOCKS5 proxy (RFC 1928) with optional username/password authentication (RFC 1929).
/// </summary>
public sealed class Socks5StreamProvider : IMqttClientStreamProvider
{
    readonly Socks5ProxyOptions _options;

    public Socks5StreamProvider(Socks5ProxyOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrEmpty(_options.Host))
        {
            throw new ArgumentException("Socks5ProxyOptions.Host must be set.", nameof(options));
        }

        if (_options.Port <= 0 || _options.Port > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(options), _options.Port, "Socks5ProxyOptions.Port must be in the range 1..65535.");
        }

        if (_options.HandshakeTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), _options.HandshakeTimeout, "Socks5ProxyOptions.HandshakeTimeout must be greater than zero.");
        }

        if (string.IsNullOrEmpty(_options.Username) && _options.Password is { Length: > 0 })
        {
            throw new ArgumentException("Socks5ProxyOptions.Password cannot be set without Socks5ProxyOptions.Username.", nameof(options));
        }
    }

    public async Task<Stream> ConnectAsync(EndPoint brokerEndPoint, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(brokerEndPoint);

        using var timeoutCts = new CancellationTokenSource(_options.HandshakeTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        var handshakeToken = linkedCts.Token;

        var addressFamily = _options.AddressFamily;
        Socket socket = addressFamily == AddressFamily.Unspecified
            ? new Socket(SocketType.Stream, ProtocolType.Tcp)
            : new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);

        NetworkStream networkStream = null;
        try
        {
            try
            {
                await socket.ConnectAsync(_options.Host, _options.Port, handshakeToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                throw new MqttProxyException(
                    MqttProxyErrorCode.ProxyUnreachable,
                    $"Timed out connecting to SOCKS5 proxy after {_options.HandshakeTimeout}.",
                    _options.ToString());
            }
            catch (Exception ex) when (ex is SocketException or IOException)
            {
                throw new MqttProxyException(
                    MqttProxyErrorCode.ProxyUnreachable,
                    $"Could not connect to SOCKS5 proxy: {ex.Message}",
                    _options.ToString(),
                    ex);
            }

            networkStream = new NetworkStream(socket, ownsSocket: true);

            try
            {
                await PerformGreetingAndAuthAsync(networkStream, handshakeToken).ConfigureAwait(false);
                await PerformConnectAsync(networkStream, brokerEndPoint, handshakeToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                throw new MqttProxyException(
                    MqttProxyErrorCode.ProxyProtocolError,
                    $"Timed out during SOCKS5 handshake after {_options.HandshakeTimeout}.",
                    _options.ToString());
            }
            catch (EndOfStreamException ex)
            {
                throw new MqttProxyException(
                    MqttProxyErrorCode.ProxyProtocolError,
                    "The SOCKS5 proxy closed the connection during the handshake.",
                    _options.ToString(),
                    ex);
            }
            catch (Exception ex) when (ex is IOException or SocketException)
            {
                throw new MqttProxyException(
                    MqttProxyErrorCode.ProxyProtocolError,
                    $"I/O error during SOCKS5 handshake: {ex.Message}",
                    _options.ToString(),
                    ex);
            }

            // Hand off the stream — ownership transfers to the caller.
            var result = new Socks5NetworkStream(networkStream, socket.LocalEndPoint, socket.RemoteEndPoint);
            networkStream = null;
            socket = null;
            return result;
        }
        finally
        {
            networkStream?.Dispose();
            socket?.Dispose();
        }
    }

    async Task PerformGreetingAndAuthAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var hasCredentials = !string.IsNullOrEmpty(_options.Username);

        // VER NMETHODS METHOD[1..n]
        // We always offer no-auth; we additionally offer user/pass when credentials are configured.
        byte[] greeting;
        if (hasCredentials)
        {
            greeting = [Socks5Protocol.Version, 0x02, Socks5Protocol.MethodNoAuth, Socks5Protocol.MethodUsernamePassword];
        }
        else
        {
            greeting = [Socks5Protocol.Version, 0x01, Socks5Protocol.MethodNoAuth];
        }

        await stream.WriteAsync(greeting, cancellationToken).ConfigureAwait(false);

        // VER METHOD
        var greetingResponse = new byte[2];
        await ReadExactlyAsync(stream, greetingResponse, cancellationToken).ConfigureAwait(false);

        if (greetingResponse[0] != Socks5Protocol.Version)
        {
            throw new MqttProxyException(
                MqttProxyErrorCode.ProxyProtocolError,
                $"Unexpected SOCKS version in greeting response: 0x{greetingResponse[0]:X2}.",
                _options.ToString());
        }

        var chosenMethod = greetingResponse[1];
        switch (chosenMethod)
        {
            case Socks5Protocol.MethodNoAuth:
                if (hasCredentials)
                {
                    // Server accepted no-auth even though we offered credentials: fine, continue.
                }

                return;

            case Socks5Protocol.MethodUsernamePassword:
                if (!hasCredentials)
                {
                    throw new MqttProxyException(
                        MqttProxyErrorCode.ProxyProtocolError,
                        "The SOCKS5 server selected username/password authentication but no credentials are configured.",
                        _options.ToString());
                }

                await PerformUsernamePasswordAuthAsync(stream, cancellationToken).ConfigureAwait(false);
                return;

            case Socks5Protocol.MethodNoneAcceptable:
                throw new MqttProxyException(
                    MqttProxyErrorCode.ProxyAuthMethodRejected,
                    "The SOCKS5 server rejected all offered authentication methods.",
                    _options.ToString());

            default:
                throw new MqttProxyException(
                    MqttProxyErrorCode.ProxyProtocolError,
                    $"The SOCKS5 server selected an unsupported authentication method: 0x{chosenMethod:X2}.",
                    _options.ToString());
        }
    }

    async Task PerformUsernamePasswordAuthAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var usernameBytes = Encoding.UTF8.GetBytes(_options.Username ?? string.Empty);
        var passwordBytes = _options.Password ?? [];

        if (usernameBytes.Length > Socks5Protocol.MaxStringLength)
        {
            throw new MqttProxyException(
                MqttProxyErrorCode.ProxyAuthFailed,
                $"Username exceeds {Socks5Protocol.MaxStringLength} bytes (RFC 1929).",
                _options.ToString());
        }

        if (passwordBytes.Length > Socks5Protocol.MaxStringLength)
        {
            throw new MqttProxyException(
                MqttProxyErrorCode.ProxyAuthFailed,
                $"Password exceeds {Socks5Protocol.MaxStringLength} bytes (RFC 1929).",
                _options.ToString());
        }

        var request = new byte[3 + usernameBytes.Length + passwordBytes.Length];
        var offset = 0;
        request[offset++] = Socks5Protocol.AuthSubNegotiationVersion;
        request[offset++] = (byte)usernameBytes.Length;
        Buffer.BlockCopy(usernameBytes, 0, request, offset, usernameBytes.Length);
        offset += usernameBytes.Length;
        request[offset++] = (byte)passwordBytes.Length;
        Buffer.BlockCopy(passwordBytes, 0, request, offset, passwordBytes.Length);

        await stream.WriteAsync(request, cancellationToken).ConfigureAwait(false);

        var response = new byte[2];
        await ReadExactlyAsync(stream, response, cancellationToken).ConfigureAwait(false);

        if (response[0] != Socks5Protocol.AuthSubNegotiationVersion)
        {
            throw new MqttProxyException(
                MqttProxyErrorCode.ProxyProtocolError,
                $"Unexpected SOCKS5 auth sub-negotiation version: 0x{response[0]:X2}.",
                _options.ToString());
        }

        if (response[1] != 0x00)
        {
            throw new MqttProxyException(
                MqttProxyErrorCode.ProxyAuthFailed,
                $"The SOCKS5 server rejected the supplied credentials (status 0x{response[1]:X2}).",
                _options.ToString());
        }
    }

    async Task PerformConnectAsync(NetworkStream stream, EndPoint brokerEndPoint, CancellationToken cancellationToken)
    {
        var request = BuildConnectRequest(brokerEndPoint, out var targetDescription);

        await stream.WriteAsync(request, cancellationToken).ConfigureAwait(false);

        // Reply: VER REP RSV ATYP BND.ADDR BND.PORT
        var header = new byte[4];
        await ReadExactlyAsync(stream, header, cancellationToken).ConfigureAwait(false);

        if (header[0] != Socks5Protocol.Version)
        {
            throw new MqttProxyException(
                MqttProxyErrorCode.ProxyProtocolError,
                $"Unexpected SOCKS version in CONNECT response: 0x{header[0]:X2}.",
                _options.ToString());
        }

        var rep = header[1];
        if (rep != Socks5Protocol.ReplySucceeded)
        {
            var (code, description) = MapReplyCode(rep);
            throw new MqttProxyException(
                code,
                $"SOCKS5 CONNECT to '{targetDescription}' failed: {description}.",
                _options.ToString());
        }

        // Discard BND.ADDR / BND.PORT. The length depends on ATYP and we tolerate unexpected lengths.
        var atyp = header[3];
        switch (atyp)
        {
            case Socks5Protocol.AddressTypeIPv4:
                await SkipAsync(stream, 4 + 2, cancellationToken).ConfigureAwait(false);
                break;

            case Socks5Protocol.AddressTypeIPv6:
                await SkipAsync(stream, 16 + 2, cancellationToken).ConfigureAwait(false);
                break;

            case Socks5Protocol.AddressTypeDomainName:
                var lenBuf = new byte[1];
                await ReadExactlyAsync(stream, lenBuf, cancellationToken).ConfigureAwait(false);
                await SkipAsync(stream, lenBuf[0] + 2, cancellationToken).ConfigureAwait(false);
                break;

            default:
                throw new MqttProxyException(
                    MqttProxyErrorCode.ProxyProtocolError,
                    $"Unexpected SOCKS5 address type in CONNECT response: 0x{atyp:X2}.",
                    _options.ToString());
        }
    }

    byte[] BuildConnectRequest(EndPoint brokerEndPoint, out string targetDescription)
    {
        // Layout: VER CMD RSV ATYP DST.ADDR DST.PORT
        if (brokerEndPoint is DnsEndPoint dns)
        {
            if (_options.ResolveDnsRemotely)
            {
                return BuildDomainConnectRequest(dns.Host, dns.Port, out targetDescription);
            }

            var addresses = ResolveDns(dns.Host, dns.Port);
            return BuildIpConnectRequest(addresses, dns.Port, out targetDescription);
        }

        if (brokerEndPoint is IPEndPoint ip)
        {
            return BuildIpConnectRequest([ip.Address], ip.Port, out targetDescription);
        }

        throw new MqttProxyException(
            MqttProxyErrorCode.ProxyAddressTypeNotSupported,
            $"Broker endpoint of type '{brokerEndPoint.GetType().Name}' is not supported by the SOCKS5 stream provider.",
            _options.ToString());
    }

    IPAddress[] ResolveDns(string host, int port)
    {
        try
        {
            var addresses = _options.AddressFamily == AddressFamily.Unspecified
                ? Dns.GetHostAddresses(host)
                : Dns.GetHostAddresses(host, _options.AddressFamily);

            if (addresses == null || addresses.Length == 0)
            {
                throw new MqttProxyException(
                    MqttProxyErrorCode.ProxyHostUnreachable,
                    $"DNS resolution for '{host}:{port}' returned no addresses.",
                    _options.ToString());
            }

            return addresses;
        }
        catch (SocketException ex)
        {
            throw new MqttProxyException(
                MqttProxyErrorCode.ProxyHostUnreachable,
                $"Local DNS resolution failed for '{host}:{port}': {ex.Message}",
                _options.ToString(),
                ex);
        }
    }

    byte[] BuildIpConnectRequest(IPAddress[] addresses, int port, out string targetDescription)
    {
        var preferredFamily = _options.AddressFamily == AddressFamily.Unspecified
            ? AddressFamily.InterNetwork
            : _options.AddressFamily;

        IPAddress chosen = null;
        foreach (var address in addresses)
        {
            if (address.AddressFamily == preferredFamily)
            {
                chosen = address;
                break;
            }
        }

        chosen ??= addresses[0];

        targetDescription = $"{chosen}:{port}";

        if (chosen.AddressFamily == AddressFamily.InterNetwork)
        {
            var addr = chosen.GetAddressBytes();
            var request = new byte[4 + 4 + 2];
            request[0] = Socks5Protocol.Version;
            request[1] = Socks5Protocol.CommandConnect;
            request[2] = Socks5Protocol.Reserved;
            request[3] = Socks5Protocol.AddressTypeIPv4;
            Buffer.BlockCopy(addr, 0, request, 4, 4);
            BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(8, 2), (ushort)port);
            return request;
        }

        if (chosen.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var addr = chosen.GetAddressBytes();
            var request = new byte[4 + 16 + 2];
            request[0] = Socks5Protocol.Version;
            request[1] = Socks5Protocol.CommandConnect;
            request[2] = Socks5Protocol.Reserved;
            request[3] = Socks5Protocol.AddressTypeIPv6;
            Buffer.BlockCopy(addr, 0, request, 4, 16);
            BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(20, 2), (ushort)port);
            return request;
        }

        throw new MqttProxyException(
            MqttProxyErrorCode.ProxyAddressTypeNotSupported,
            $"IP address family '{chosen.AddressFamily}' is not supported by SOCKS5.",
            _options.ToString());
    }

    byte[] BuildDomainConnectRequest(string host, int port, out string targetDescription)
    {
        var hostBytes = Encoding.UTF8.GetBytes(host ?? string.Empty);
        if (hostBytes.Length == 0)
        {
            throw new MqttProxyException(
                MqttProxyErrorCode.ProxyHostUnreachable,
                "Broker host is empty.",
                _options.ToString());
        }

        if (hostBytes.Length > Socks5Protocol.MaxStringLength)
        {
            throw new MqttProxyException(
                MqttProxyErrorCode.ProxyAddressTypeNotSupported,
                $"Broker host exceeds the SOCKS5 DOMAINNAME limit of {Socks5Protocol.MaxStringLength} bytes.",
                _options.ToString());
        }

        targetDescription = $"{host}:{port}";

        var request = new byte[4 + 1 + hostBytes.Length + 2];
        request[0] = Socks5Protocol.Version;
        request[1] = Socks5Protocol.CommandConnect;
        request[2] = Socks5Protocol.Reserved;
        request[3] = Socks5Protocol.AddressTypeDomainName;
        request[4] = (byte)hostBytes.Length;
        Buffer.BlockCopy(hostBytes, 0, request, 5, hostBytes.Length);
        BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(5 + hostBytes.Length, 2), (ushort)port);
        return request;
    }

    static (MqttProxyErrorCode Code, string Description) MapReplyCode(byte rep)
    {
        return rep switch
        {
            Socks5Protocol.ReplyGeneralFailure => (MqttProxyErrorCode.ProxyGeneralFailure, "general SOCKS server failure"),
            Socks5Protocol.ReplyConnectionNotAllowed => (MqttProxyErrorCode.ProxyConnectionNotAllowed, "connection not allowed by ruleset"),
            Socks5Protocol.ReplyNetworkUnreachable => (MqttProxyErrorCode.ProxyNetworkUnreachable, "network unreachable"),
            Socks5Protocol.ReplyHostUnreachable => (MqttProxyErrorCode.ProxyHostUnreachable, "host unreachable"),
            Socks5Protocol.ReplyConnectionRefused => (MqttProxyErrorCode.ProxyConnectionRefused, "connection refused"),
            Socks5Protocol.ReplyTtlExpired => (MqttProxyErrorCode.ProxyTtlExpired, "TTL expired"),
            Socks5Protocol.ReplyCommandNotSupported => (MqttProxyErrorCode.ProxyCommandNotSupported, "command not supported"),
            Socks5Protocol.ReplyAddressTypeNotSupported => (MqttProxyErrorCode.ProxyAddressTypeNotSupported, "address type not supported"),
            _ => (MqttProxyErrorCode.ProxyUnknownError, $"unknown reply code 0x{rep:X2}")
        };
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

    static async Task SkipAsync(NetworkStream stream, int count, CancellationToken cancellationToken)
    {
        if (count <= 0)
        {
            return;
        }

        var buffer = new byte[count];
        await ReadExactlyAsync(stream, buffer, cancellationToken).ConfigureAwait(false);
    }
}
