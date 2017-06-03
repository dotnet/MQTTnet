using System;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;

namespace MQTTnet.Core.Server
{
    public sealed class MqttServerOptions
    {
        public DefaultEndpointOptions DefaultEndpointOptions { get; } = new DefaultEndpointOptions();

        public SslEndpointOptions SslEndpointOptions { get; } = new SslEndpointOptions();
        
        public int ConnectionBacklog { get; set; } = 10;

        public TimeSpan DefaultCommunicationTimeout { get; set; } = TimeSpan.FromSeconds(10);

        public Func<MqttConnectPacket, MqttConnectReturnCode> ConnectionValidator { get; set; }
    }
}
