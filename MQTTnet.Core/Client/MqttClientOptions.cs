using MQTTnet.Core.Serializer;
using System;

namespace MQTTnet.Core.Client
{
    public interface IMqttClientOptions
    {
        MqttClientTlsOptions TlsOptions { get; set; }

        MqttApplicationMessage WillMessage { get; set; }

        string UserName { get; set; }

        string Password { get; set; }

        string ClientId { get; set; }

        bool CleanSession { get; set; }

        TimeSpan KeepAlivePeriod { get; set; }

        TimeSpan DefaultCommunicationTimeout { get; set; }

        MqttProtocolVersion ProtocolVersion { get; set; }
    }
}
