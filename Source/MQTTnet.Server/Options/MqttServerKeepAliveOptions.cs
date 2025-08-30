// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Server;

public sealed class MqttServerKeepAliveOptions
{
    /// <summary>
    ///     When this mode is enabled the MQTT server will not close a connection when the
    ///     client is currently sending a (large) payload. This may lead to "dead" connections
    ///     When this mode is disabled the MQTT server will disconnect a client when the keep
    ///     alive timeout is reached even if the client is currently sending a (large) payload.
    /// </summary>
    public bool DisconnectClientWhenReadingPayload { get; set; }

    public TimeSpan MonitorInterval { get; set; } = TimeSpan.FromMilliseconds(500);
}