namespace MQTTnet.Core.Server
{
    public sealed class GetOrCreateClientSessionResult
    {
        public bool IsExistingSession { get; set; }

        public MqttClientSession Session { get; set; }
    }
}
