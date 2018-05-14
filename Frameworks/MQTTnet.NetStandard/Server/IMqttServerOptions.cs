using System;

namespace MQTTnet.Server
{
    public interface IMqttServerOptions
    {
        int ConnectionBacklog { get; }

        int MaxPendingMessagesPerClient { get; }
        MqttPendingMessagesOverflowStrategy PendingMessagesOverflowStrategy { get; }
        /// <summary>
        /// How long server keeps sessions in memory after last packet/KeepAlive receeived. Be generous, if this setting is shorter than KeepAlive, it will close even Live connections!
        /// </summary>
        TimeSpan StaleSessionLifetime { get; }

        TimeSpan DefaultCommunicationTimeout { get; }

        Action<MqttConnectionValidatorContext> ConnectionValidator { get; }
        Action<MqttSubscriptionInterceptorContext> SubscriptionInterceptor { get; }
        Action<MqttApplicationMessageInterceptorContext> ApplicationMessageInterceptor { get; }

        MqttServerDefaultEndpointOptions DefaultEndpointOptions { get; }
        MqttServerTlsEndpointOptions TlsEndpointOptions { get; }

        IMqttServerStorage Storage { get; }
    }
}