using System;
using MQTTnet.Formatter;

namespace MQTTnet.Client.Options
{
    public interface IMqttClientOptions
    {
        string ClientId { get; }
        bool CleanSession { get; }
        IMqttClientCredentials Credentials { get; }
        MqttProtocolVersion ProtocolVersion { get; }
        IMqttClientChannelOptions ChannelOptions { get; }
        
        TimeSpan CommunicationTimeout { get; }
        TimeSpan KeepAlivePeriod { get; }
        TimeSpan? KeepAliveSendInterval { get; }
        
        MqttApplicationMessage WillMessage { get; }
        uint? WillDelayInterval { get; }

        string AuthenticationMethod { get; }
        byte[] AuthenticationData { get; }
        uint? MaximumPacketSize { get; }
        ushort? ReceiveMaximum { get; }
        bool? RequestProblemInformation { get; }
        bool? RequestResponseInformation { get; }
        uint? SessionExpiryInterval { get; }
        ushort? TopicAliasMaximum { get; }
    }
}