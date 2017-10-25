using System;
using MQTTnet.Core.Serializer;

namespace MQTTnet.Core.Client
{
    public interface IMqttClientOptions
    {
        bool CleanSession { get; }
        string ClientId { get; }
        TimeSpan DefaultCommunicationTimeout { get; }
        TimeSpan KeepAlivePeriod { get; }
        string Password { get; }
        MqttProtocolVersion ProtocolVersion { get; }
        MqttClientTlsOptions TlsOptions { get; }
        string UserName { get; }
        MqttApplicationMessage WillMessage { get; }
    }
}