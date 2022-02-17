// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Packets;
using MQTTnet.Server;

namespace MQTTnet.Formatter
{
    public sealed class MqttSubAckPacketFactory
    {
        public MqttSubAckPacket Create(MqttSubscribePacket subscribePacket, SubscribeResult subscribeResult)
        {
            if (subscribePacket == null)
            {
                throw new ArgumentNullException(nameof(subscribePacket));
            }

            if (subscribeResult == null)
            {
                throw new ArgumentNullException(nameof(subscribeResult));
            }

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
}