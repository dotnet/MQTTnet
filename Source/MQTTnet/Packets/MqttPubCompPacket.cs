// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Protocol;

namespace MQTTnet.Packets;

public sealed class MqttPubCompPacket : MqttPacketWithIdentifier
{
    /// <summary>
    ///     Added in MQTTv5.
    /// </summary>
    public MqttPubCompReasonCode ReasonCode { get; set; } = MqttPubCompReasonCode.Success;

    /// <summary>
    ///     Added in MQTTv5.
    /// </summary>
    public string ReasonString { get; set; }

    /// <summary>
    ///     Added in MQTTv5.
    /// </summary>
    public List<MqttUserProperty> UserProperties { get; set; }

    public override string ToString()
    {
        return $"PubComp: [PacketIdentifier={PacketIdentifier}] [ReasonCode={ReasonCode}]";
    }
}