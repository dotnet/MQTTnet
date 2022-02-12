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
    public sealed class MqttPubAckPacketFactory
    {
        public MqttPubAckPacket Create(MqttPublishPacket publishPacket, InterceptingPublishEventArgs interceptingPublishEventArgs)
        {
            if (publishPacket == null)
            {
                throw new ArgumentNullException(nameof(publishPacket));
            }

            var pubAckPacket = new MqttPubAckPacket
            {
                PacketIdentifier = publishPacket.PacketIdentifier,
                ReasonCode = MqttPubAckReasonCode.Success
            };

            if (interceptingPublishEventArgs != null)
            {
                pubAckPacket.ReasonCode = (MqttPubAckReasonCode)(int)interceptingPublishEventArgs.Response.ReasonCode;
                pubAckPacket.ReasonString = interceptingPublishEventArgs.Response.ReasonString;
                pubAckPacket.UserProperties = interceptingPublishEventArgs.Response.UserProperties;
            }

            return pubAckPacket;
        }

        public MqttPubAckPacket Create(MqttApplicationMessageReceivedEventArgs applicationMessageReceivedEventArgs)
        {
            if (applicationMessageReceivedEventArgs == null)
            {
                throw new ArgumentNullException(nameof(applicationMessageReceivedEventArgs));
            }

            var pubAckPacket = Create(applicationMessageReceivedEventArgs.PublishPacket, applicationMessageReceivedEventArgs.ReasonCode);
            pubAckPacket.UserProperties = applicationMessageReceivedEventArgs.ResponseUserProperties;
            pubAckPacket.ReasonString = applicationMessageReceivedEventArgs.ResponseReasonString;

            return pubAckPacket;
        }

        static MqttPubAckPacket Create(MqttPublishPacket publishPacket, MqttApplicationMessageReceivedReasonCode applicationMessageReceivedReasonCode)
        {
            if (publishPacket == null)
            {
                throw new ArgumentNullException(nameof(publishPacket));
            }

            var pubAckPacket = new MqttPubAckPacket
            {
                PacketIdentifier = publishPacket.PacketIdentifier,
                ReasonCode = (MqttPubAckReasonCode)(int)applicationMessageReceivedReasonCode
            };

            return pubAckPacket;
        }
    }
}