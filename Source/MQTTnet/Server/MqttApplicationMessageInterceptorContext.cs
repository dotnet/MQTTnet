using MQTTnet.Packets;

namespace MQTTnet.Server
{
    public class MqttApplicationMessageInterceptorContext
    {
        public MqttApplicationMessageInterceptorContext(string clientId, MqttConnectPacket connectPacket, MqttApplicationMessage applicationMessage)
        {
            ClientId = clientId;
            ConnectPacket = connectPacket;
            ApplicationMessage = applicationMessage;
        }

        public string ClientId { get; }

        /// <summary>
        /// the connect packet used to authentificate this connection, will be null if the message was published by the server
        /// </summary>
        public MqttConnectPacket ConnectPacket { get; }
        public MqttApplicationMessage ApplicationMessage { get; set; }

        public bool AcceptPublish { get; set; } = true;

        public bool CloseConnection { get; set; }
    }
}
