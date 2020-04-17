using System.Collections.Generic;
using MQTTnet.Packets;

namespace MQTTnet.Client.Subscribing
{
    public class MqttClientSubscribeOptions
    {
        public List<MqttTopicFilter> TopicFilters { get; set; } = new List<MqttTopicFilter>();

        public uint? SubscriptionIdentifier { get; set; }

        public List<MqttUserProperty> UserProperties { get; set; }
    }
}
