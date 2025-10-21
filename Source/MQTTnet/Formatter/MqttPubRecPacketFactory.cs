// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Formatter;

public static class MqttPubRecPacketFactory
{
    public static MqttPubRecPacket Create(MqttApplicationMessageReceivedEventArgs applicationMessageReceivedEventArgs)
    {
        ArgumentNullException.ThrowIfNull(applicationMessageReceivedEventArgs);

        var pubRecPacket = Create(applicationMessageReceivedEventArgs.PublishPacket, applicationMessageReceivedEventArgs.ReasonCode);
        pubRecPacket.UserProperties = applicationMessageReceivedEventArgs.ResponseUserProperties;

        return pubRecPacket;
    }

    static MqttPubRecPacket Create(MqttPublishPacket publishPacket, MqttApplicationMessageReceivedReasonCode applicationMessageReceivedReasonCode)
    {
        var pubRecPacket = new MqttPubRecPacket
        {
            PacketIdentifier = publishPacket.PacketIdentifier,
            ReasonCode = (MqttPubRecReasonCode)(int)applicationMessageReceivedReasonCode
        };

        return pubRecPacket;
    }
}