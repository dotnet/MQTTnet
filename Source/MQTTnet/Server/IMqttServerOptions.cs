using System;
using MQTTnet.Server.ExtendedAuthenticationExchange;

namespace MQTTnet.Server
{
    public interface IMqttServerOptions
    {
        string ClientId { get; set; }

        bool EnablePersistentSessions { get; }

        IMqttEnhancedAuthenticationBrokerHandler EnhancedAuthenticationBrokerHandler { get; set; }

        int MaxPendingMessagesPerClient { get; }

        MqttPendingMessagesOverflowStrategy PendingMessagesOverflowStrategy { get; }

        TimeSpan DefaultCommunicationTimeout { get; }

        IMqttServerConnectionValidator ConnectionValidator { get; }

        IMqttServerSubscriptionInterceptor SubscriptionInterceptor { get; }

        IMqttServerUnsubscriptionInterceptor UnsubscriptionInterceptor { get; }

        IMqttServerApplicationMessageInterceptor ApplicationMessageInterceptor { get; }

        IMqttServerClientMessageQueueInterceptor ClientMessageQueueInterceptor { get; }

        MqttServerTcpEndpointOptions DefaultEndpointOptions { get; }

        MqttServerTlsTcpEndpointOptions TlsEndpointOptions { get; }

        IMqttServerStorage Storage { get; }

        IMqttRetainedMessagesManager RetainedMessagesManager { get; }
    }
}