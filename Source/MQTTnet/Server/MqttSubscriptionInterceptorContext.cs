using System;
using System.Collections.Generic;
using MQTTnet.Packets;

namespace MQTTnet.Server
{
    public class MqttSubscriptionInterceptorContext : MqttBaseInterceptorContext
    {
        public MqttSubscriptionInterceptorContext(string clientId, TopicFilter topicFilter, MqttConnectPacket connectPacket, IDictionary<object, object> sessionItems) : base(connectPacket, sessionItems)
        {
            ClientId = clientId;
            TopicFilter = topicFilter ?? throw new ArgumentNullException(nameof(topicFilter));
        }

        public string ClientId { get; }

        public TopicFilter TopicFilter { get; set; }

        public bool AcceptSubscription { get; set; } = true;

        public bool CloseConnection { get; set; }
    }
}
