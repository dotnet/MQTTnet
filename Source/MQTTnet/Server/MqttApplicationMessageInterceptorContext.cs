using System.Collections.Generic;
using MQTTnet.Packets;

namespace MQTTnet.Server
{
    public class MqttApplicationMessageInterceptorContext : MqttBaseInterceptorContext
    {
        public MqttApplicationMessageInterceptorContext(string clientId, IDictionary<object, object> sessionItems, MqttConnectPacket connectPacket, MqttApplicationMessage applicationMessage) : base(connectPacket, sessionItems)
        {
            ClientId = clientId;
            ApplicationMessage = applicationMessage;
        }

        public string ClientId { get; }

        public MqttApplicationMessage ApplicationMessage { get; set; }

        public bool AcceptPublish { get; set; } = true;

        public bool CloseConnection { get; set; }
    }
}
