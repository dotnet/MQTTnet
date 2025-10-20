// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MQTTnet.Packets;

public static class MqttPacketExtensions
{
    public static string GetRfcName(this MqttPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        return packet switch
        {
            MqttConnectPacket => "CONNECT",
            MqttConnAckPacket => "CONNACK",
            MqttAuthPacket => "AUTH",
            MqttDisconnectPacket => "DISCONNECT",
            MqttPingReqPacket => "PINGREQ",
            MqttPingRespPacket => "PINGRESP",
            MqttSubscribePacket => "SUBSCRIBE",
            MqttSubAckPacket => "SUBACK",
            MqttUnsubscribePacket => "UNSUBSCRIBE",
            MqttUnsubAckPacket => "UNSUBACK",
            MqttPublishPacket => "PUBLISH",
            MqttPubAckPacket => "PUBACK",
            MqttPubRelPacket => "PUBREL",
            MqttPubRecPacket => "PUBREC",
            MqttPubCompPacket => "PUBCOMP",
            _ => packet.GetType().Name
        };
    }
}