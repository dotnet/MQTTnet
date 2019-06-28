using MQTTnet.Packets;
using System;

namespace MQTTnet.Server
{
    public class MqttSubscriptionInterceptorContext
    {
        public MqttSubscriptionInterceptorContext(MqttConnectPacket connectPacket, TopicFilter topicFilter)
        {
            ClientId = connectPacket.ClientId;
            ConnectPacket = connectPacket;
            TopicFilter = topicFilter ?? throw new ArgumentNullException(nameof(topicFilter));
        }

        public string ClientId { get; }
        public MqttConnectPacket ConnectPacket { get; }
        public TopicFilter TopicFilter { get; set; }
        
        public bool AcceptSubscription { get; set; } = true;

        public bool CloseConnection { get; set; }
    }
}
