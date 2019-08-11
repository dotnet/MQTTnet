﻿using System.Net.Security;
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

        public IMqttServerCertificateCredentials CertificateCredentials { get; set; }

        public bool ClientCertificateRequired { get; set; }

        public bool CheckCertificateRevocation { get; set; }
        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }

        public SslProtocols SslProtocol { get; set; } = SslProtocols.Tls12;
    }
}
