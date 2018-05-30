namespace MQTTnet.Server
{
    public class GetOrCreateClientSessionResult
    {
        public bool IsExistingSession { get; set; }

        public MqttClientSession Session { get; set; }
    }
}
