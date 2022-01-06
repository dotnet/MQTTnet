namespace MQTTnet.Server
{
    public sealed class MqttInjectedApplicationMessage
    {
        public string SenderClientId { get; set; }

        public MqttApplicationMessage ApplicationMessage { get; set; }
    }
}