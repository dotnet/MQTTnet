// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using MQTTnet.Packets;
using MQTTnet.Server;

namespace MQTTnet.Formatter
{
    public sealed class MqttUnsubAckPacketFactory
    {
        public MqttUnsubAckPacket Create(MqttUnsubscribePacket unsubscribePacket, UnsubscribeResult unsubscribeResult)
        {
            if (unsubscribePacket == null)
            {
                throw new ArgumentNullException(nameof(unsubscribePacket));
            }

            if (unsubscribeResult == null)
            {
                throw new ArgumentNullException(nameof(unsubscribeResult));
            }

            var unsubAckPacket = new MqttUnsubAckPacket
            {
                PacketIdentifier = unsubscribePacket.PacketIdentifier
            };

            // MQTTv5.0.0 only.
            unsubAckPacket.ReasonCodes = unsubscribeResult.ReasonCodes;

            return unsubAckPacket;
        }
    }
}