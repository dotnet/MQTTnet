﻿using System;
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
            pubRecPacket.Properties.UserProperties.AddRange(applicationMessageReceivedEventArgs.ResponseUserProperties);

            return pubRecPacket;
        }

        public MqttBasePacket Create(MqttPublishPacket publishPacket, MqttApplicationMessageResponse applicationMessageResponse)
        {
            if (publishPacket == null) throw new ArgumentNullException(nameof(publishPacket));

            var pubRecPacket = new MqttPubRecPacket
            {
                PacketIdentifier = publishPacket.PacketIdentifier,
                ReasonCode = (MqttPubRecReasonCode) (int) applicationMessageResponse.ReasonCode,
                Properties =
                {
                    ReasonString = applicationMessageResponse.ReasonString
                }
            };

            pubRecPacket.Properties.UserProperties.AddRange(applicationMessageResponse.UserProperties);
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