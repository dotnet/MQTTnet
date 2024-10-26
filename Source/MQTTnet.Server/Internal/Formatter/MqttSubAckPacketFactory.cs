// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Packets;

namespace MQTTnet.Server.Internal.Formatter;

public static class MqttSubAckPacketFactory
{
    public static MqttSubAckPacket Create(MqttSubscribePacket subscribePacket, SubscribeResult subscribeResult)
    {
        ArgumentNullException.ThrowIfNull(subscribePacket);
        ArgumentNullException.ThrowIfNull(subscribeResult);

        var subAckPacket = new MqttSubAckPacket
        {
            PacketIdentifier = subscribePacket.PacketIdentifier,
            ReasonCodes = subscribeResult.ReasonCodes,
            ReasonString = subscribeResult.ReasonString,
            UserProperties = subscribeResult.UserProperties
        };

        return subAckPacket;
    }
}