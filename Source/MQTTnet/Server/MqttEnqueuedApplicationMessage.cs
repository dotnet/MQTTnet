namespace MQTTnet.Server
{
    public class MqttEnqueuedApplicationMessage
    {
        public MqttEnqueuedApplicationMessage(MqttClientSession sender, MqttApplicationMessage applicationMessage)
        {
            Sender = sender;
            ApplicationMessage = applicationMessage;
        }

        public MqttClientSession Sender { get; }

        public MqttApplicationMessage ApplicationMessage { get; }
    }
}