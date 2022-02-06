// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using MQTTnet.Exceptions;
using MQTTnet.Packets;

namespace MQTTnet.Client
{
    public sealed class MqttClientUnsubscribeResultFactory
    {
        public MqttClientUnsubscribeResult Create(MqttUnsubscribePacket unsubscribePacket, MqttUnsubAckPacket unsubAckPacket)
        {
            if (unsubscribePacket == null) throw new ArgumentNullException(nameof(unsubscribePacket));
            if (unsubAckPacket == null) throw new ArgumentNullException(nameof(unsubAckPacket));

            // MQTTv3.1.1 has no reason code at all!
            if (unsubAckPacket.ReasonCodes.Count != 0 && unsubAckPacket.ReasonCodes.Count != unsubscribePacket.TopicFilters.Count)
            {
                throw new MqttProtocolViolationException(
                    "The return codes are not matching the topic filters [MQTT-3.9.3-1].");
            }

            var result = new MqttClientUnsubscribeResult
            {
                PacketIdentifier = unsubAckPacket.PacketIdentifier,
                ReasonString = unsubAckPacket.Properties.ReasonString
            };

            result.UserProperties.AddRange(unsubAckPacket.Properties.UserProperties);
            
            for (var i = 0; i < unsubscribePacket.TopicFilters.Count; i++)
            {
                result.Items.Add(CreateUnsubscribeResultItem(i, unsubscribePacket, unsubAckPacket));
            }

            return result;
        }
        
        static MqttClientUnsubscribeResultItem CreateUnsubscribeResultItem(int index, MqttUnsubscribePacket unsubscribePacket, MqttUnsubAckPacket unsubAckPacket)
        {
            var resultCode = MqttClientUnsubscribeResultCode.Success;
            
            if (unsubAckPacket.ReasonCodes.Any())
            {
                // MQTTv3.1.1 has no reason code and no return code!.
                resultCode = (MqttClientUnsubscribeResultCode) unsubAckPacket.ReasonCodes[index];
            }
            
            return new MqttClientUnsubscribeResultItem
            {
                TopicFilter = unsubscribePacket.TopicFilters[index],
                ResultCode = resultCode
            };
        }
    }
}