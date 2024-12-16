// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server.Internal.Formatter;

public static class MqttPubRecPacketFactory
{
    public static MqttPacket Create(MqttPublishPacket publishPacket, DispatchApplicationMessageResult dispatchApplicationMessageResult)
    {
        ArgumentNullException.ThrowIfNull(publishPacket);

        var pubRecPacket = new MqttPubRecPacket
        {
            PacketIdentifier = publishPacket.PacketIdentifier,
            ReasonCode = (MqttPubRecReasonCode)dispatchApplicationMessageResult.ReasonCode,
            ReasonString = dispatchApplicationMessageResult.ReasonString,
            UserProperties = dispatchApplicationMessageResult.UserProperties
        };

        return pubRecPacket;
    }
}