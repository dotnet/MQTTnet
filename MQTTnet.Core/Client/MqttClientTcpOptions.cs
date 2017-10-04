namespace MQTTnet.Core.Client
{
    public class MqttClientTcpOptions : MqttClientOptions
    {
        public string Server { get; set; }

        public int? Port { get; set; }
    }
}
