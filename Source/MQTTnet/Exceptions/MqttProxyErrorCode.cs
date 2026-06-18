// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Exceptions;

/// <summary>
///     Categorized error codes for proxy-level failures surfaced via <see cref="MqttProxyException"/>.
///     The first set of values map 1:1 to SOCKS5 REP codes (RFC 1928 §6); additional values cover
///     non-protocol conditions (connecting to the proxy itself, authentication, protocol violations).
/// </summary>
public enum MqttProxyErrorCode
{
    /// <summary>The error category could not be classified.</summary>
    ProxyUnknownError = 0,

    /// <summary>The TCP connection to the proxy could not be established (DNS, refused, timeout).</summary>
    ProxyUnreachable,

    /// <summary>The proxy returned a response that does not conform to the expected protocol.</summary>
    ProxyProtocolError,

    /// <summary>The proxy rejected every authentication method offered by the client (SOCKS5 method = 0xFF).</summary>
    ProxyAuthMethodRejected,

    /// <summary>The proxy rejected the supplied credentials.</summary>
    ProxyAuthFailed,

    /// <summary>SOCKS5 REP = 0x01: general SOCKS server failure.</summary>
    ProxyGeneralFailure,

    /// <summary>SOCKS5 REP = 0x02: connection not allowed by ruleset.</summary>
    ProxyConnectionNotAllowed,

    /// <summary>SOCKS5 REP = 0x03: network unreachable.</summary>
    ProxyNetworkUnreachable,

    /// <summary>SOCKS5 REP = 0x04: host unreachable.</summary>
    ProxyHostUnreachable,

    /// <summary>SOCKS5 REP = 0x05: connection refused by destination.</summary>
    ProxyConnectionRefused,

    /// <summary>SOCKS5 REP = 0x06: TTL expired.</summary>
    ProxyTtlExpired,

    /// <summary>SOCKS5 REP = 0x07: command not supported by the proxy.</summary>
    ProxyCommandNotSupported,

    /// <summary>SOCKS5 REP = 0x08: address type not supported by the proxy.</summary>
    ProxyAddressTypeNotSupported
}
