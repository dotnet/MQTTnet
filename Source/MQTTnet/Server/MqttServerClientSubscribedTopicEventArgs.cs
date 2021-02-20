using System;

namespace MQTTnet.Server
{
    public class MqttServerClientSubscribedTopicEventArgs : EventArgs
    {
        public MqttServerClientSubscribedTopicEventArgs(string clientId, MqttTopicFilter topicFilter)
        {
            ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            TopicFilter = topicFilter ?? throw new ArgumentNullException(nameof(topicFilter));
        }

        /// <summary>
        /// Gets the client identifier.
        /// Hint: This identifier needs to be unique over all used clients / devices on the broker to avoid connection issues.
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        /// Gets the topic filter.
        /// The topic filter can contain topics and wildcards.
        /// </summary>
        public MqttTopicFilter TopicFilter { get; }
    }
}
