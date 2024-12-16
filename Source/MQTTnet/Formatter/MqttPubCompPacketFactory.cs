// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Formatter;

public static class MqttPubCompPacketFactory
{
    public static MqttPubCompPacket Create(MqttPubRelPacket pubRelPacket, MqttApplicationMessageReceivedReasonCode reasonCode)
    {
        ArgumentNullException.ThrowIfNull(pubRelPacket);

        return new MqttPubCompPacket
        {
            PacketIdentifier = pubRelPacket.PacketIdentifier,
            ReasonCode = (MqttPubCompReasonCode)(int)reasonCode
        };
    }
}