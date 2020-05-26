using System.Security.Cryptography.X509Certificates;

namespace MQTTnet.Certificates
{
    public interface ICertificateProvider
    {
        X509Certificate2 GetCertificate();
    }
}
