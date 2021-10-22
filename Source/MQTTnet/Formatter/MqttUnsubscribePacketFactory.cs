using System;
using MQTTnet.Client.Unsubscribing;
using MQTTnet.Packets;

namespace MQTTnet.Formatter
{
    public sealed class MqttUnsubscribePacketFactory
    {
        public MqttUnsubscribePacket Create(MqttClientUnsubscribeOptions clientUnsubscribeOptions)
        {
            if (clientUnsubscribeOptions == null) throw new ArgumentNullException(nameof(clientUnsubscribeOptions));

            var packet = new MqttUnsubscribePacket();

            if (clientUnsubscribeOptions.TopicFilters != null)
            {
                packet.TopicFilters.AddRange(clientUnsubscribeOptions.TopicFilters);
            }

            if (clientUnsubscribeOptions.UserProperties != null)
            {
                packet.Properties.UserProperties.AddRange(clientUnsubscribeOptions.UserProperties);
            }

            return packet;
        }
    }
}