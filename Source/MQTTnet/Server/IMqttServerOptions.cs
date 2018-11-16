using System;

namespace MQTTnet.Server
{
    public interface IMqttServerOptions
    {
        bool EnablePersistentSessions { get; }

        int MaxPendingMessagesPerClient { get; }
        MqttPendingMessagesOverflowStrategy PendingMessagesOverflowStrategy { get; }

        TimeSpan DefaultCommunicationTimeout { get; }

        Action<MqttConnectionValidatorContext> ConnectionValidator { get; }
        Action<MqttSubscriptionInterceptorContext> SubscriptionInterceptor { get; }
        Action<MqttApplicationMessageInterceptorContext> ApplicationMessageInterceptor { get; }
        Action<MqttClientMessageQueueInterceptorContext> ClientMessageQueueInterceptor { get; }

        MqttServerTcpEndpointOptions DefaultEndpointOptions { get; }
        MqttServerTlsTcpEndpointOptions TlsEndpointOptions { get; }

        IMqttServerStorage Storage { get; }
    }
}