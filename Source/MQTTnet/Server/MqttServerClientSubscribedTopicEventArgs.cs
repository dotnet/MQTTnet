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

        public string ClientId { get; }

        public MqttTopicFilter TopicFilter { get; }
    }
}
