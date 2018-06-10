using System;

namespace MQTTnet.Server
{
    public class MqttServerOptions : IMqttServerOptions
    {
        public MqttServerTcpEndpointOptions DefaultEndpointOptions { get; } = new MqttServerTcpEndpointOptions();

        public MqttServerTlsTcpEndpointOptions TlsEndpointOptions { get; } = new MqttServerTlsTcpEndpointOptions();

        public bool EnablePersistentSessions { get; set; }

        public int MaxPendingMessagesPerClient { get; set; } = 250;
        public MqttPendingMessagesOverflowStrategy PendingMessagesOverflowStrategy { get; set; } = MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage;

        public TimeSpan DefaultCommunicationTimeout { get; set; } = TimeSpan.FromSeconds(15);

        public Action<MqttConnectionValidatorContext> ConnectionValidator { get; set; }

        public Action<MqttApplicationMessageInterceptorContext> ApplicationMessageInterceptor { get; set; }

        public Action<MqttClientMessageQueueInterceptorContext> ClientMessageQueueInterceptor { get; set; }

        public Action<MqttSubscriptionInterceptorContext> SubscriptionInterceptor { get; set; }

        public IMqttServerStorage Storage { get; set; }
    }
}
