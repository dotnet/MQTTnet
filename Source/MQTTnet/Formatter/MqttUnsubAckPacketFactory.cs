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
        public MqttUnsubAckPacket Create(MqttUnsubscribePacket unsubscribePacket, MqttUnsubscribeResult mqttUnsubscribeResult)
        {
            if (unsubscribePacket == null) throw new ArgumentNullException(nameof(unsubscribePacket));
            if (mqttUnsubscribeResult == null) throw new ArgumentNullException(nameof(mqttUnsubscribeResult));

            var unsubAckPacket = new MqttUnsubAckPacket
            {
                PacketIdentifier = unsubscribePacket.PacketIdentifier
            };

            // MQTTv5.0.0 only.
            unsubAckPacket.ReasonCodes.AddRange(mqttUnsubscribeResult.ReasonCodes);
            
            return unsubAckPacket;
        }
    }
}