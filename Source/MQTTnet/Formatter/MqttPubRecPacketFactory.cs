// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Client;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace MQTTnet.Formatter
{
    public sealed class MqttPubRecPacketFactory
    {
        public MqttPubRecPacket Create(MqttApplicationMessageReceivedEventArgs applicationMessageReceivedEventArgs)
        {
            if (applicationMessageReceivedEventArgs == null) throw new ArgumentNullException(nameof(applicationMessageReceivedEventArgs));

            var pubRecPacket = Create(applicationMessageReceivedEventArgs.PublishPacket, applicationMessageReceivedEventArgs.ReasonCode);
            pubRecPacket.UserProperties = applicationMessageReceivedEventArgs.ResponseUserProperties;

            return pubRecPacket;
        }

        public MqttBasePacket Create(MqttPublishPacket publishPacket, PublishResponse applicationMessageResponse)
        {
            if (publishPacket == null) throw new ArgumentNullException(nameof(publishPacket));

            var pubRecPacket = new MqttPubRecPacket
            {
                PacketIdentifier = publishPacket.PacketIdentifier,
                ReasonCode = (MqttPubRecReasonCode) (int) applicationMessageResponse.ReasonCode,
                ReasonString = applicationMessageResponse.ReasonString,
                UserProperties = applicationMessageResponse.UserProperties
            };

            return pubRecPacket;
        }

        MqttPubRecPacket Create(MqttPublishPacket publishPacket, MqttApplicationMessageReceivedReasonCode applicationMessageReceivedReasonCode)
        {
            if (publishPacket == null) throw new ArgumentNullException(nameof(publishPacket));

            var pubRecPacket = new MqttPubRecPacket
            {
                PacketIdentifier = publishPacket.PacketIdentifier,
                ReasonCode = (MqttPubRecReasonCode) (int) applicationMessageReceivedReasonCode
            };

            return pubRecPacket;
        }
    }
}