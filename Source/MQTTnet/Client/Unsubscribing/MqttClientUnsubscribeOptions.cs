using System.Collections.Generic;
using MQTTnet.Packets;

namespace MQTTnet.Client.Unsubscribing
{
    public class MqttClientUnsubscribeOptions
    {
        public List<string> TopicFilters { get; set; } = new List<string>();

        public List<MqttUserProperty> UserProperties { get; set; } = new List<MqttUserProperty>();
    }
}
