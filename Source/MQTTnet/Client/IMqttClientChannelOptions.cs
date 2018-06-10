namespace MQTTnet.Client
{
    public interface IMqttClientChannelOptions
    {
        MqttClientTlsOptions TlsOptions { get; }
    }
}
