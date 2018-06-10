namespace MQTTnet.Server
{
    public class MqttClientMessageQueueInterceptorContext
    {
        public MqttClientMessageQueueInterceptorContext(string senderClientId, string receiverClientId, MqttApplicationMessage applicationMessage)
        {
            SenderClientId = senderClientId;
            ReceiverClientId = receiverClientId;
            ApplicationMessage = applicationMessage;
        }

        public string SenderClientId { get; }

        public string ReceiverClientId { get; }

        public MqttApplicationMessage ApplicationMessage { get; set; }

        public bool AcceptEnqueue { get; set; } = true;
    }
}
