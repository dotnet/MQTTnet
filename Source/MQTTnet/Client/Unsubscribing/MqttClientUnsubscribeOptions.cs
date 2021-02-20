using System.Collections.Generic;
using MQTTnet.Packets;

namespace MQTTnet.Client.Unsubscribing
{
    public class MqttClientUnsubscribeOptions
    {
        /// <summary>
        /// Gets or sets a list of topic filters the client wants to unsubscribe from.
        /// Topic filters can include regular topics or wild cards.
        /// </summary>
        public List<string> TopicFilters { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the user properties.
        /// In MQTT 5, user properties are basic UTF-8 string key-value pairs that you can append to almost every type of MQTT packet.
        /// As long as you don’t exceed the maximum message size, you can use an unlimited number of user properties to add metadata to MQTT messages and pass information between publisher, broker, and subscriber.
        /// The feature is very similar to the HTTP header concept.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public List<MqttUserProperty> UserProperties { get; set; } = new List<MqttUserProperty>();
    }
}
