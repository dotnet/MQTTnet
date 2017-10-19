using MQTTnet.Core.Serializer;
using System;

namespace MQTTnet.Core.Client
{
    public class MqttClientTcpOptions : IMqttClientOptions
    {
        public MqttClientTlsOptions TlsOptions { get; set; } = new MqttClientTlsOptions();

        public MqttApplicationMessage WillMessage { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public string ClientId { get; set; } = Guid.NewGuid().ToString().Replace("-", string.Empty);

        public bool CleanSession { get; set; } = true;

        public TimeSpan KeepAlivePeriod { get; set; } = TimeSpan.FromSeconds(5);

        public TimeSpan DefaultCommunicationTimeout { get; set; } = TimeSpan.FromSeconds(10);

        public MqttProtocolVersion ProtocolVersion { get; set; } = MqttProtocolVersion.V311;


        public string Server { get; set; }

        public int? Port { get; set; }
    }
}
