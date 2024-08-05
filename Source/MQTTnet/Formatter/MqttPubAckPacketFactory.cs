// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Formatter;

public static class MqttPubAckPacketFactory
{
    public static MqttPubAckPacket Create(MqttApplicationMessageReceivedEventArgs applicationMessageReceivedEventArgs)
    {
        if (applicationMessageReceivedEventArgs == null)
        {
            throw new ArgumentNullException(nameof(applicationMessageReceivedEventArgs));
        }

        var pubAckPacket = new MqttPubAckPacket
        {
            PacketIdentifier = applicationMessageReceivedEventArgs.PublishPacket.PacketIdentifier,
            ReasonCode = (MqttPubAckReasonCode)(int)applicationMessageReceivedEventArgs.ReasonCode,
            UserProperties = applicationMessageReceivedEventArgs.ResponseUserProperties,
            ReasonString = applicationMessageReceivedEventArgs.ResponseReasonString
        };

        return pubAckPacket;
    }
}