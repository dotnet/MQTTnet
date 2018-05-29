using System;
using MQTTnet.Serializer;

namespace MQTTnet.Client
{
    public interface IMqttClientOptions
    {
        string ClientId { get; }

        IMqttClientCredentials Credentials { get; }
        bool CleanSession { get; }
        MqttApplicationMessage WillMessage { get; }
        
        TimeSpan CommunicationTimeout { get; }
        TimeSpan KeepAlivePeriod { get; }
        TimeSpan? KeepAliveSendInterval { get; }

        MqttProtocolVersion ProtocolVersion { get; }

        IMqttClientChannelOptions ChannelOptions { get; }
    }
}