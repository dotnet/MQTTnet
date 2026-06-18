// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;

namespace MQTTnet.Extensions.Socks5;

/// <summary>
///     Options for the SOCKS5 stream provider (RFC 1928 / RFC 1929).
///     A new provider can be constructed from these options via
///     <see cref="MqttClientOptionsBuilderExtensions.WithSocks5Proxy(MqttClientOptionsBuilder, Socks5ProxyOptions)"/>.
/// </summary>
public sealed class Socks5ProxyOptions
{
    /// <summary>
    ///     The SOCKS5 proxy host name or IP literal. Must be set.
    /// </summary>
    public string Host { get; set; }

    /// <summary>
    ///     The SOCKS5 proxy port. Defaults to 1080.
    /// </summary>
    public int Port { get; set; } = 1080;

    /// <summary>
    ///     The user name used for the username/password authentication sub-negotiation (RFC 1929).
    ///     When <c>null</c> or empty, the client will only offer the "no authentication" method.
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    ///     The password (raw bytes per RFC 1929) used for the username/password authentication
    ///     sub-negotiation. Must be 1..255 bytes when <see cref="Username"/> is set.
    /// </summary>
    public byte[] Password { get; set; }

    /// <summary>
    ///     When <c>true</c> (the default) the broker hostname is forwarded to the proxy as a
    ///     SOCKS5 <c>DOMAINNAME</c> address type, so DNS resolution happens at the proxy
    ///     (typical when the broker is behind the proxy in a private network).
    ///     When <c>false</c>, the hostname is resolved locally before sending the CONNECT request
    ///     and an IPv4 / IPv6 address type is used.
    /// </summary>
    public bool ResolveDnsRemotely { get; set; } = true;

    /// <summary>
    ///     The maximum time allowed for the full SOCKS5 handshake (TCP connect to proxy + greeting
    ///     + optional auth + CONNECT). The handshake will be cancelled and a
    ///     <see cref="MQTTnet.Exceptions.MqttProxyException"/> will be thrown when this elapses.
    ///     Defaults to 30 seconds.
    /// </summary>
    public TimeSpan HandshakeTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     Address family preference for the socket connecting to the proxy and for local DNS
    ///     resolution when <see cref="ResolveDnsRemotely"/> is <c>false</c>.
    /// </summary>
    public AddressFamily AddressFamily { get; set; } = AddressFamily.Unspecified;

    public override string ToString()
    {
        return $"{Host}:{Port}";
    }
}
