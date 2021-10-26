using System;
using System.Linq;
using MQTTnet.Exceptions;
using MQTTnet.Packets;

namespace MQTTnet.Client.Subscribing
{
    public sealed class MqttClientSubscribeResultFactory
    {
        public MqttClientSubscribeResult Create(MqttSubscribePacket subscribePacket, MqttSubAckPacket subAckPacket)
        {
            if (subscribePacket == null) throw new ArgumentNullException(nameof(subscribePacket));
            if (subAckPacket == null) throw new ArgumentNullException(nameof(subAckPacket));
            
            // MQTTv5.0.0 handling.
            if (subAckPacket.ReasonCodes.Any() && subAckPacket.ReasonCodes.Count != subscribePacket.TopicFilters.Count)
            {
                throw new MqttProtocolViolationException(
                    "The reason codes are not matching the topic filters [MQTT-3.9.3-1].");
            }
            
            var result = new MqttClientSubscribeResult
            {
                ReasonString = subAckPacket.Properties.ReasonString
            };

            result.UserProperties.AddRange(subAckPacket.Properties.UserProperties);
            
            for (var i = 0; i < subscribePacket.TopicFilters.Count; i++)
            {
                result.Items.Add(CreateSubscribeResultItem(i, subscribePacket, subAckPacket));
            }
            
            return result;
        }

        static MqttClientSubscribeResultItem CreateSubscribeResultItem(int index, MqttSubscribePacket subscribePacket, MqttSubAckPacket subAckPacket)
        {
            var resultCode = (MqttClientSubscribeResultCode) subAckPacket.ReasonCodes[index];

            return new MqttClientSubscribeResultItem
            {
                TopicFilter = subscribePacket.TopicFilters[index],
                ResultCode = resultCode
            };
        }
    }
}