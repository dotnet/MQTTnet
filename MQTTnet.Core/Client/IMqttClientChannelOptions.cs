namespace MQTTnet.Core.Client
{
    public interface IMqttClientChannelOptions
    {
        MqttClientTlsOptions TlsOptions { get; }
    }
}
