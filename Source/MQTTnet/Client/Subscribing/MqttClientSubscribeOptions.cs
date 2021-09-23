using System.Collections.Generic;
using MQTTnet.Packets;

namespace MQTTnet.Client.Subscribing
{
    public class MqttClientSubscribeOptions
    {
        /// <summary>
        /// Gets or sets a list of topic filters the client wants to subscribe to.
        /// Topic filters can include regular topics or wild cards.
        /// </summary>
        public List<MqttTopicFilter> TopicFilters { get; set; } = new List<MqttTopicFilter>();

        /// <summary>
        /// Gets or sets the subscription identifier.
        /// The client can specify a subscription identifier when subscribing.
        /// The broker will establish and store the mapping relationship between this subscription and subscription identifier when successfully create or modify subscription.
        /// The broker will return the subscription identifier associated with this PUBLISH packet and the PUBLISH packet to the client when need to forward PUBLISH packets matching this subscription to this client.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public uint SubscriptionIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the user properties.
        /// In MQTT 5, user properties are basic UTF-8 string key-value pairs that you can append to almost every type of MQTT packet.
        /// As long as you don’t exceed the maximum message size, you can use an unlimited number of user properties to add metadata to MQTT messages and pass information between publisher, broker, and subscriber.
        /// The feature is very similar to the HTTP header concept.
        /// Hint: MQTT 5 feature only.
        /// </summary>
        public List<MqttUserProperty> UserProperties { get; set; }
    }
}
