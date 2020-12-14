namespace MQTTnet.Server
{
    public sealed class MqttPendingApplicationMessage
    {
        public MqttPendingApplicationMessage(MqttApplicationMessage applicationMessage, MqttClientConnection sender)
        {
            Sender = sender;
            ApplicationMessage = applicationMessage;
        }

        public MqttClientConnection Sender { get; }

        public MqttApplicationMessage ApplicationMessage { get; }
    }
}