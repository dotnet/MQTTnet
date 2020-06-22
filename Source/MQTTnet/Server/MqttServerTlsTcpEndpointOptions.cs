using System;
using System.Security.Authentication;
using MQTTnet.Certificates;

namespace MQTTnet.Server
{
    public class MqttServerTlsTcpEndpointOptions : MqttServerTcpEndpointBaseOptions
    {
        ICertificateProvider _certificateProvider;

        public MqttServerTlsTcpEndpointOptions()
        {
            Port = 8883;
        }

        [Obsolete("Please use CertificateProvider with 'BlobCertificateProvider' instead.")]
        public byte[] Certificate { get; set; }

        [Obsolete("Please use CertificateProvider with 'BlobCertificateProvider' including password property instead.")]
        public IMqttServerCertificateCredentials CertificateCredentials { get; set; }

#if !WINDOWS_UWP
        public System.Net.Security.RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }
#endif
        public ICertificateProvider CertificateProvider
        {
            get
            {
                // Backward compatibility only. Gets converted to auto property when
                // obsolete properties are removed.
                if (_certificateProvider != null)
                {
                    return _certificateProvider;
                }

                if (Certificate == null)
                {
                    return null;
                }

                return new BlobCertificateProvider(Certificate)
                {
                    Password = CertificateCredentials?.Password
                };
            }

            set => _certificateProvider = value;
        }

        public bool ClientCertificateRequired { get; set; }

        public bool CheckCertificateRevocation { get; set; }

        public SslProtocols SslProtocol { get; set; } = SslProtocols.Tls12;
    }
}
