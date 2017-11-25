using System;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public class MqttServerOptions
    {
        public MqttServerDefaultEndpointOptions DefaultEndpointOptions { get; } = new MqttServerDefaultEndpointOptions();

        public MqttServerTlsEndpointOptions TlsEndpointOptions { get; } = new MqttServerTlsEndpointOptions();

        public int ConnectionBacklog { get; set; } = 10;
        
        public TimeSpan DefaultCommunicationTimeout { get; set; } = TimeSpan.FromSeconds(15);

        public Func<MqttConnectPacket, MqttConnectReturnCode> ConnectionValidator { get; set; }

        public Action<MqttApplicationMessageInterceptorContext> ApplicationMessageInterceptor { get; set; }

        public Action<MqttSubscriptionInterceptorContext> SubscriptionInterceptor { get; set; }

        public IMqttServerStorage Storage { get; set; }
    }
}
