using System;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;

namespace MQTTnet.Core.Server
{
    public class MqttServerOptions
    {
        public int Port { get; set; } = 1883;

        public int ConnectionBacklog { get; set; } = 10;

        public TimeSpan DefaultCommunicationTimeout { get; set; } = TimeSpan.FromSeconds(10);

        public Func<MqttConnectPacket, MqttConnectReturnCode> ConnectionValidator { get; set; }

        public bool UseSSL = false;

        /// <summary>
        /// The path to the X509 SSL certificate.
        /// </summary>
        public string CertificatePath = string.Empty;
    }
}
