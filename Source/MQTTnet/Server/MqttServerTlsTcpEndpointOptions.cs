using System.Security.Authentication;

namespace MQTTnet.Server
{
    public class MqttServerTlsTcpEndpointOptions : MqttServerTcpEndpointBaseOptions
    {
        public MqttServerTlsTcpEndpointOptions()
        {
            Port = 8883;
        }

        public byte[] Certificate { get; set; }


        public SslProtocols SslProtocol { get; set; } = SslProtocols.Tls12;
    }
}
