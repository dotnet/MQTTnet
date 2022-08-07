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
            if (applicationMessageReceivedEventArgs == null)
            {
                throw new ArgumentNullException(nameof(applicationMessageReceivedEventArgs));
            }

            var pubRecPacket = Create(applicationMessageReceivedEventArgs.PublishPacket, applicationMessageReceivedEventArgs.ReasonCode);
            pubRecPacket.UserProperties = applicationMessageReceivedEventArgs.ResponseUserProperties;

            return pubRecPacket;
        }

        public MqttPacket Create(
            MqttPublishPacket publishPacket,
            InterceptingPublishEventArgs interceptingPublishEventArgs,
            DispatchApplicationMessageResult dispatchApplicationMessageResult)
        {
            if (publishPacket == null)
            {
                throw new ArgumentNullException(nameof(publishPacket));
            }

            var pubRecPacket = new MqttPubRecPacket
            {
                PacketIdentifier = publishPacket.PacketIdentifier,
                ReasonCode = MqttPubRecReasonCode.Success
            };

            if (interceptingPublishEventArgs != null)
            {
                pubRecPacket.ReasonCode = (MqttPubRecReasonCode)(int)interceptingPublishEventArgs.Response.ReasonCode;
                pubRecPacket.ReasonString = interceptingPublishEventArgs.Response.ReasonString;
                pubRecPacket.UserProperties = interceptingPublishEventArgs.Response.UserProperties;
            }

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
}