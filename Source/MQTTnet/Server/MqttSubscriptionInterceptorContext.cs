using System;

namespace MQTTnet.Server
{
    public class MqttSubscriptionInterceptorContext
    {
        public MqttSubscriptionInterceptorContext(string clientId, TopicFilter topicFilter)
        {
            ClientId = clientId;
            TopicFilter = topicFilter ?? throw new ArgumentNullException(nameof(topicFilter));
        }

        public string ClientId { get; }

        public TopicFilter TopicFilter { get; }
        
        public bool AcceptSubscription { get; set; } = true;

        public bool CloseConnection { get; set; }
    }
}
