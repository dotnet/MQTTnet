namespace MQTTnet.Server
{
    public class MqttEnqueuedApplicationMessage
    {
        public MqttEnqueuedApplicationMessage(MqttApplicationMessage applicationMessage, MqttClientConnection sender)
        {
            Sender = sender;
            ApplicationMessage = applicationMessage;
        }

        public MqttClientConnection Sender { get; }

        public MqttApplicationMessage ApplicationMessage { get; }
    }
}