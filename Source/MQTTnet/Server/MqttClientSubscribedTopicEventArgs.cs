using System;

namespace MQTTnet.Server
{
    public class MqttClientSubscribedTopicEventArgs : EventArgs
    {
        public MqttClientSubscribedTopicEventArgs(string clientId, TopicFilter topicFilter)
        {
            ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            TopicFilter = topicFilter ?? throw new ArgumentNullException(nameof(topicFilter));
        }

        public string ClientId { get; }

        public TopicFilter TopicFilter { get; }
    }
}
