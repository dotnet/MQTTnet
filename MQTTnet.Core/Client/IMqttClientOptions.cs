using System;
using MQTTnet.Core.Serializer;

namespace MQTTnet.Core.Client
{
    public interface IMqttClientOptions
    {
        string ClientId { get; }
        bool CleanSession { get; }
        MqttApplicationMessage WillMessage { get; }
        IMqttClientCredentials Credentials { get; }

        TimeSpan DefaultCommunicationTimeout { get; }
        TimeSpan KeepAlivePeriod { get; }
        MqttProtocolVersion ProtocolVersion { get; }
        MqttClientTlsOptions TlsOptions { get; }
    }
}