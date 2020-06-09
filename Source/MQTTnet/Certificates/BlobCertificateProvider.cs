using System;
using System.Security.Cryptography.X509Certificates;

namespace MQTTnet.Certificates
{
    public class BlobCertificateProvider : ICertificateProvider
    {
        public BlobCertificateProvider(byte[] blob)
        {
            Blob = blob ?? throw new ArgumentNullException(nameof(blob));
        }

        public byte[] Blob { get; }

        public string Password { get; set; }

        public X509Certificate2 GetCertificate()
        {
            if (string.IsNullOrEmpty(Password))
            {
                // Use a different overload when no password is specified. Otherwise the constructor will fail.
                return new X509Certificate2(Blob);
            }

            return new X509Certificate2(Blob, Password);
        }
    }
}
