using System.Collections.Generic;
using MQTTnet.Packets;

namespace MQTTnet.Client.Subscribing
{
    public class MqttClientSubscribeOptions
    {
        public List<TopicFilter> TopicFilters { get; set; } = new List<TopicFilter>();

        public uint? SubscriptionIdentifier { get; set; }

        public List<MqttUserProperty> UserProperties { get; set; } = new List<MqttUserProperty>();
    }
}
