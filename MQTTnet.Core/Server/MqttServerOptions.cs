using System;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;

namespace MQTTnet.Core.Server
{
    public sealed class MqttServerOptions
    {
        public MqttServerDefaultEndpointOptions DefaultEndpointOptions { get; } = new MqttServerDefaultEndpointOptions();

        public MqttServerTlsEndpointOptions TlsEndpointOptions { get; } = new MqttServerTlsEndpointOptions();
        
        public int ConnectionBacklog { get; set; } = 10;

        public TimeSpan DefaultCommunicationTimeout { get; set; } = TimeSpan.FromSeconds(15);

        public Func<MqttConnectPacket, MqttConnectReturnCode> ConnectionValidator { get; set; }
    }
}
