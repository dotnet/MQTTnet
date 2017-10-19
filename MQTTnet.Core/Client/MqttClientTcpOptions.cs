namespace MQTTnet.Core.Client
{
    public class MqttClientTcpOptions : BaseMqttClientOptions
    {
        public string Server { get; set; }

        public int? Port { get; set; }
    }
}
