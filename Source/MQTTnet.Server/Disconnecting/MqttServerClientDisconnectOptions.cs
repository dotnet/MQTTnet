// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server;

public sealed class MqttServerClientDisconnectOptions
{
    public MqttDisconnectReasonCode ReasonCode { get; set; } = MqttDisconnectReasonCode.NormalDisconnection;

    /// <summary>
    ///     The reason string is sent to every client via a DISCONNECT packet.
    ///     <remarks>MQTT 5.0.0+ feature.</remarks>
    /// </summary>
    public string ReasonString { get; set; }

    /// <summary>
    ///     The server reference is sent to every client via a DISCONNECT packet.
    ///     <remarks>MQTT 5.0.0+ feature.</remarks>
    /// </summary>
    public string ServerReference { get; set; }

    /// <summary>
    ///     These user properties are sent to every client via a DISCONNECT packet.
    ///     <remarks>MQTT 5.0.0+ feature.</remarks>
    /// </summary>
    public List<MqttUserProperty> UserProperties { get; set; }
}