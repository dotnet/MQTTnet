namespace MQTTnet.Core.Client
{
    public class MqttClientTcpOptions : IMqttClientChannelOptions
    {
        public string Server { get; set; }

        public int? Port { get; set; }

        public MqttClientTlsOptions TlsOptions { get; set; } = new MqttClientTlsOptions();
    }
}
