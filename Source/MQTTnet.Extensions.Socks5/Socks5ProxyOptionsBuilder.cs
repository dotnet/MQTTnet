// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;
using System.Text;

namespace MQTTnet.Extensions.Socks5;

/// <summary>
///     Fluent builder for <see cref="Socks5ProxyOptions"/>.
/// </summary>
public sealed class Socks5ProxyOptionsBuilder
{
    readonly Socks5ProxyOptions _options = new();

    public Socks5ProxyOptions Build()
    {
        if (string.IsNullOrEmpty(_options.Host))
        {
            throw new InvalidOperationException("A SOCKS5 proxy host must be configured via WithHost.");
        }

        if (_options.Port <= 0 || _options.Port > 65535)
        {
            throw new InvalidOperationException("The SOCKS5 proxy port must be in the range 1..65535.");
        }

        return _options;
    }

    public Socks5ProxyOptionsBuilder WithHost(string host)
    {
        _options.Host = host ?? throw new ArgumentNullException(nameof(host));
        return this;
    }

    public Socks5ProxyOptionsBuilder WithPort(int port)
    {
        _options.Port = port;
        return this;
    }

    public Socks5ProxyOptionsBuilder WithCredentials(string username, string password)
    {
        _options.Username = username;
        _options.Password = password != null ? Encoding.UTF8.GetBytes(password) : null;
        return this;
    }

    public Socks5ProxyOptionsBuilder WithCredentials(string username, byte[] password)
    {
        _options.Username = username;
        _options.Password = password;
        return this;
    }

    /// <summary>
    ///     Controls whether the broker hostname is forwarded to the proxy (remote DNS, default) or
    ///     resolved locally before issuing the CONNECT request.
    /// </summary>
    public Socks5ProxyOptionsBuilder WithResolveDnsRemotely(bool resolveDnsRemotely = true)
    {
        _options.ResolveDnsRemotely = resolveDnsRemotely;
        return this;
    }

    public Socks5ProxyOptionsBuilder WithHandshakeTimeout(TimeSpan timeout)
    {
        _options.HandshakeTimeout = timeout;
        return this;
    }

    public Socks5ProxyOptionsBuilder WithAddressFamily(AddressFamily addressFamily)
    {
        _options.AddressFamily = addressFamily;
        return this;
    }
}
