// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server.Formatter;

public static class MqttPubAckPacketFactory
{
    public static MqttPubAckPacket Create(MqttPublishPacket publishPacket, DispatchApplicationMessageResult dispatchApplicationMessageResult)
    {
        if (publishPacket == null)
        {
            throw new ArgumentNullException(nameof(publishPacket));
        }

        if (dispatchApplicationMessageResult == null)
        {
            throw new ArgumentNullException(nameof(dispatchApplicationMessageResult));
        }

        var pubAckPacket = new MqttPubAckPacket
        {
            PacketIdentifier = publishPacket.PacketIdentifier,
            ReasonCode = (MqttPubAckReasonCode)dispatchApplicationMessageResult.ReasonCode,
            ReasonString = dispatchApplicationMessageResult.ReasonString,
            UserProperties = dispatchApplicationMessageResult.UserProperties
        };

        return pubAckPacket;
    }
}