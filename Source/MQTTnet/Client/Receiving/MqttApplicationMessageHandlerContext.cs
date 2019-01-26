namespace MQTTnet.Client.Receiving
{
    public class MqttApplicationMessageHandlerContext
    {
        public MqttApplicationMessageHandlerContext(string senderClientId, MqttApplicationMessage applicationMessage)
        {
            SenderClientId = senderClientId;
            ApplicationMessage = applicationMessage;
        }

        public string SenderClientId { get; }

        public MqttApplicationMessage ApplicationMessage { get; }
    }
}
