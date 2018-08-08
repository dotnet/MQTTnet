namespace MQTTnet.Server
{
    public class MqttServerTlsTcpEndpointOptions : MqttServerTcpEndpointBaseOptions
    {
        public MqttServerTlsTcpEndpointOptions()
        {
            Port = 8883;
        }

        public byte[] Certificate { get; set; }
    }
}
