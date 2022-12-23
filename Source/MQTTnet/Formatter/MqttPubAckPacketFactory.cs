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
        public MqttPubAckPacket Create(
            MqttPublishPacket publishPacket,
            DispatchApplicationMessageResult dispatchApplicationMessageResult)
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

        public MqttPubAckPacket Create(MqttApplicationMessageReceivedEventArgs applicationMessageReceivedEventArgs)
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
}