using MQTTnet.Client.ExtendedAuthenticationExchange;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using System;
using System.Collections.Generic;

namespace MQTTnet.Client.Options
{
    public class MqttClientOptions : IMqttClientOptions
    {
        public string ClientId { get; set; } = Guid.NewGuid().ToString("N");
        public bool CleanSession { get; set; } = true;
        public IMqttClientCredentials Credentials { get; set; }
        public IMqttExtendedAuthenticationExchangeHandler ExtendedAuthenticationExchangeHandler { get; set; }
        public MqttProtocolVersion ProtocolVersion { get; set; } = MqttProtocolVersion.V311;

        public IMqttClientChannelOptions ChannelOptions { get; set; }
        public TimeSpan CommunicationTimeout { get; set; } = TimeSpan.FromSeconds(10);
        public TimeSpan KeepAlivePeriod { get; set; } = TimeSpan.FromSeconds(15);

        public MqttApplicationMessage WillMessage { get; set; }
        public uint? WillDelayInterval { get; set; }

        public string AuthenticationMethod { get; set; }
        public byte[] AuthenticationData { get; set; }

        public uint? MaximumPacketSize { get; set; }
        public ushort? ReceiveMaximum { get; set; }
        public bool? RequestProblemInformation { get; set; }
        public bool? RequestResponseInformation { get; set; }
        public uint? SessionExpiryInterval { get; set; }
        public ushort? TopicAliasMaximum { get; set; }
        public List<MqttUserProperty> UserProperties { get; set; }
    }
}
