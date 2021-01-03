using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace MQTTnet.Client.Options
{
    public class MqttClientCertificateValidationCallbackContext
    {
        /// <summary>
        /// Gets or sets the certificate.
        /// </summary>
        public X509Certificate Certificate { get; set; }

        /// <summary>
        /// Gets or sets the certificate chain.
        /// </summary>
        public X509Chain Chain { get; set; }

        /// <summary>
        /// Gets or sets the SSL policy errors.
        /// </summary>
        public SslPolicyErrors SslPolicyErrors { get; set; }

        /// <summary>
        /// Gets or sets the client options.
        /// </summary>
        public IMqttClientChannelOptions ClientOptions { get; set; }
    }
}
