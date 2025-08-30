// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MQTTnet.Protocol;

namespace MQTTnet.Packets;

public sealed class MqttPubRelPacket : MqttPacketWithIdentifier
{
    /// <summary>
    ///     Added in MQTTv5.
    /// </summary>
    public MqttPubRelReasonCode ReasonCode { get; set; } = MqttPubRelReasonCode.Success;

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
        return $"PubRel: [PacketIdentifier={PacketIdentifier}] [ReasonCode={ReasonCode}]";
    }
}