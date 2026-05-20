// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;

namespace MQTTnet.Channel;

/// <summary>
///     Optional marker interface that a <see cref="Stream"/> returned by an
///     <see cref="IMqttClientStreamProvider"/> may implement to expose the local and remote
///     socket endpoints to the MQTT TCP channel (used for diagnostics only).
/// </summary>
public interface IMqttClientStreamEndPoints
{
    /// <summary>
    ///     Gets the local endpoint of the socket actually opened by the provider
    ///     (e.g. the local interface used to reach the proxy). May be <c>null</c> if not known.
    /// </summary>
    EndPoint LocalEndPoint { get; }

    /// <summary>
    ///     Gets the remote endpoint of the socket actually opened by the provider
    ///     (e.g. the proxy address). May be <c>null</c> if not known.
    /// </summary>
    EndPoint RemoteEndPoint { get; }
}
