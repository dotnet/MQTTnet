using System;
using MQTTnet.Core.Serializer;

namespace MQTTnet.Core.Client
{
    public interface IMqttClientOptions
    {
        string ClientId { get; }

        IMqttClientCredentials Credentials { get; }
        bool CleanSession { get; }
        MqttApplicationMessage WillMessage { get; }
        
        TimeSpan CommunicationTimeout { get; }
        TimeSpan KeepAlivePeriod { get; }
        MqttProtocolVersion ProtocolVersion { get; }

        IMqttClientChannelOptions ChannelOptions { get; }
    }
}