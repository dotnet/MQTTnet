using System.Net.Security;
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

        public IMqttServerCredentials CertificateCredentials { get; set; }

        public bool ClientCertificateRequired { get; set; }

        public bool CheckCertificateRevocation { get; set; }

#if !WINDOWS_UWP
        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }
#endif
        public SslProtocols SslProtocol { get; set; } = SslProtocols.Tls12;
    }
}
