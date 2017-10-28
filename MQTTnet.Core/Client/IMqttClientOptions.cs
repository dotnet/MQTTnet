using System;
using MQTTnet.Core.Serializer;

namespace MQTTnet.Core.Client
{
    public interface IMqttClientOptions
    {
        string ClientId { get; }

        /// <summary>
        /// The LogId is used to create a scope to correlate logging. If no value is provided the ClientId is used instead 
        /// </summary>
        string LogId { get; }
        IMqttClientCredentials Credentials { get; }
        bool CleanSession { get; }
        MqttApplicationMessage WillMessage { get; }
        
        TimeSpan CommunicationTimeout { get; }
        TimeSpan KeepAlivePeriod { get; }
        MqttProtocolVersion ProtocolVersion { get; }

        IMqttClientChannelOptions ChannelOptions { get; }
    }
}