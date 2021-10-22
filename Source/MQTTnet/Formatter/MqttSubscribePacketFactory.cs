using System;
using MQTTnet.Client.Subscribing;
using MQTTnet.Packets;

namespace MQTTnet.Formatter
{
    public sealed class MqttSubscribePacketFactory
    {
        public MqttSubscribePacket Create(MqttClientSubscribeOptions clientSubscribeOptions)
        {
            if (clientSubscribeOptions == null) throw new ArgumentNullException(nameof(clientSubscribeOptions));

            var packet = new MqttSubscribePacket();
            packet.TopicFilters.AddRange(clientSubscribeOptions.TopicFilters);
            packet.Properties.SubscriptionIdentifier = clientSubscribeOptions.SubscriptionIdentifier;

            if (clientSubscribeOptions.UserProperties != null)
            {
                packet.Properties.UserProperties.AddRange(clientSubscribeOptions.UserProperties);
            }

            return packet;
        }
    }
}