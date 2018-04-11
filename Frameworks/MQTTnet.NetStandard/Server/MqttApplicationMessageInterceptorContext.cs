namespace MQTTnet.Server
{
    public class MqttApplicationMessageInterceptorContext
    {
        public MqttApplicationMessageInterceptorContext(string clientId, MqttApplicationMessage applicationMessage)
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
