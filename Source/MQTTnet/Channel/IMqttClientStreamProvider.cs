// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;

namespace MQTTnet.Channel;

/// <summary>
///     Produces a connected byte <see cref="Stream"/> that carries traffic to an MQTT broker endpoint.
///     This is the extension point used to plug stream-level proxies (SOCKS5, HTTP CONNECT, etc.)
///     into the TCP transport without modifying <c>MqttTcpChannel</c>.
///     Implementations are responsible for the full handshake to the broker (proxy negotiation,
///     authentication, target connect) but MUST NOT perform any TLS wrapping; TLS is layered on
///     top of the returned stream by the channel itself, so the broker host can be used as SNI.
/// </summary>
/// <remarks>
///     <para>
///         The returned <see cref="Stream"/> is owned by the caller (the MQTT channel). Disposing
///         it will release any underlying socket. Providers should therefore avoid keeping their
///         own references to the underlying socket after returning.
///     </para>
///     <para>
///         Implementations may optionally have the returned stream implement
///         <see cref="IMqttClientStreamEndPoints"/> to expose the local and remote socket
///         endpoints to <c>MqttTcpChannel</c> for diagnostics.
///     </para>
///     <para>
///         <see cref="ConnectAsync"/> is invoked on every (re)connect attempt; implementations
///         must be safe to call concurrently or sequentially across multiple client instances.
///     </para>
/// </remarks>
public interface IMqttClientStreamProvider
{
    /// <summary>
    ///     Establishes a connection to the MQTT broker <paramref name="brokerEndPoint"/>, performing
    ///     any required proxy handshake, and returns the resulting raw byte stream.
    /// </summary>
    /// <param name="brokerEndPoint">
    ///     The MQTT broker endpoint. Can be a <see cref="DnsEndPoint"/> (in which case the
    ///     provider may forward the hostname for remote DNS resolution) or an
    ///     <see cref="IPEndPoint"/>.
    /// </param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>
    ///     A connected <see cref="Stream"/> whose bytes are transported end-to-end to
    ///     <paramref name="brokerEndPoint"/>.
    /// </returns>
    Task<Stream> ConnectAsync(EndPoint brokerEndPoint, CancellationToken cancellationToken);
}
