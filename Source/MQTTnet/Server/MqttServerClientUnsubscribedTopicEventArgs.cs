using System;

namespace MQTTnet.Server
{
    public class MqttServerClientUnsubscribedTopicEventArgs : EventArgs
    {
        public MqttServerClientUnsubscribedTopicEventArgs(string clientId, string topicFilter)
        {
            ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            TopicFilter = topicFilter ?? throw new ArgumentNullException(nameof(topicFilter));
        }

        public string ClientId { get; }

        public string TopicFilter { get; }
    }
}
