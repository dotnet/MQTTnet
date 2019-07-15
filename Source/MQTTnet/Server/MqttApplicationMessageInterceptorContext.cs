namespace MQTTnet.Server
{
    public class MqttApplicationMessageInterceptorContext
    {
        public MqttApplicationMessageInterceptorContext(string clientId, MqttClientConnection clientConnection, MqttApplicationMessage applicationMessage)
        {
            ClientId = clientId;
            ApplicationMessage = applicationMessage;
            ClientConnection = clientConnection;
        }

        public string ClientId { get; }

        public MqttApplicationMessage ApplicationMessage { get; set; }

        public MqttClientConnection ClientConnection { get; }

        public bool AcceptPublish { get; set; } = true;

        public bool CloseConnection { get; set; }
    }
}
