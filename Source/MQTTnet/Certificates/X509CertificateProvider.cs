#if !WINDOWS_UWP
using System;
using System.Security.Cryptography.X509Certificates;

namespace MQTTnet.Certificates
{
    public class X509CertificateProvider : ICertificateProvider
    {
        readonly X509Certificate2 _certificate;

        public X509CertificateProvider(X509Certificate2 certificate)
        {
            _certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
        }

        public X509Certificate2 GetCertificate()
        {
            return _certificate;
        }
    }
}
#endif