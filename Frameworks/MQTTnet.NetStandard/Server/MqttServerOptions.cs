using System;

namespace MQTTnet.Server
{
    public class MqttServerOptions : IMqttServerOptions
    {
        public MqttServerDefaultEndpointOptions DefaultEndpointOptions { get; } = new MqttServerDefaultEndpointOptions();

        public MqttServerTlsEndpointOptions TlsEndpointOptions { get; } = new MqttServerTlsEndpointOptions();

        public int ConnectionBacklog { get; set; } = 10;

        public int MaxPendingMessagesPerClient { get; set; } = 250;
        
        public TimeSpan DefaultCommunicationTimeout { get; set; } = TimeSpan.FromSeconds(15);

        public Action<MqttConnectionValidatorContext> ConnectionValidator { get; set; }

        public Action<MqttApplicationMessageInterceptorContext> ApplicationMessageInterceptor { get; set; }

        public Action<MqttSubscriptionInterceptorContext> SubscriptionInterceptor { get; set; }

        public IMqttServerStorage Storage { get; set; }
    }
}
