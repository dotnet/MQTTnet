using System;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

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

        public MqttServerOptionsBuilderClientTlsParameters ClientTlsParameters  { get; set; } = new MqttServerOptionsBuilderClientTlsParameters();
    }
}
