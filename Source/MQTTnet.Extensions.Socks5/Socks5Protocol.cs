// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Extensions.Socks5;

/// <summary>
///     Protocol constants for SOCKS5 (RFC 1928) and the username/password sub-negotiation
///     (RFC 1929). Internal use only.
/// </summary>
internal static class Socks5Protocol
{
    public const byte Version = 0x05;
    public const byte AuthSubNegotiationVersion = 0x01;

    public const byte MethodNoAuth = 0x00;
    public const byte MethodGssApi = 0x01;
    public const byte MethodUsernamePassword = 0x02;
    public const byte MethodNoneAcceptable = 0xFF;

    public const byte CommandConnect = 0x01;
    public const byte Reserved = 0x00;

    public const byte AddressTypeIPv4 = 0x01;
    public const byte AddressTypeDomainName = 0x03;
    public const byte AddressTypeIPv6 = 0x04;

    public const byte ReplySucceeded = 0x00;
    public const byte ReplyGeneralFailure = 0x01;
    public const byte ReplyConnectionNotAllowed = 0x02;
    public const byte ReplyNetworkUnreachable = 0x03;
    public const byte ReplyHostUnreachable = 0x04;
    public const byte ReplyConnectionRefused = 0x05;
    public const byte ReplyTtlExpired = 0x06;
    public const byte ReplyCommandNotSupported = 0x07;
    public const byte ReplyAddressTypeNotSupported = 0x08;

    public const int MaxStringLength = 255;
}
