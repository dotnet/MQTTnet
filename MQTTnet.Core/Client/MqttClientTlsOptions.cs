using System.Collections.Generic;

namespace MQTTnet.Core.Client
{
    public sealed class MqttClientTlsOptions
    {
        public bool UseTls { get; set; }

        public bool CheckCertificateRevocation { get; set; }

        public List<byte[]> Certificates { get; set; }
    }
}
