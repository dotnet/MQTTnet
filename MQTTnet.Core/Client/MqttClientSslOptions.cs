using System.Collections.Generic;

namespace MQTTnet.Core.Client
{
    public sealed class MqttClientSslOptions
    {
        public bool UseSsl { get; set; }

        public bool CheckCertificateRevocation { get; set; }

        public List<byte[]> Certificates { get; set; }
    }
}
