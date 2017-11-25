namespace MQTTnet.Server
{
    public sealed class MqttServerDefaultEndpointOptions
    {
        public bool IsEnabled { get; set; } = true;

        public int? Port { get; set; }
    }
}
