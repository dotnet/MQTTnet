using System;

namespace MQTTnet.Server
{
    public interface IMqttServerOptions
    {
        string ClientId { get; set; }

        bool EnablePersistentSessions { get; }

        int MaxPendingMessagesPerClient { get; }
        MqttPendingMessagesOverflowStrategy PendingMessagesOverflowStrategy { get; }

        TimeSpan DefaultCommunicationTimeout { get; }

        IMqttServerConnectionValidator ConnectionValidator { get; }
        IMqttServerSubscriptionInterceptor SubscriptionInterceptor { get; }
        IMqttServerApplicationMessageInterceptor ApplicationMessageInterceptor { get; }
        IMqttServerClientMessageQueueInterceptor ClientMessageQueueInterceptor { get; }
        IMqttServerApplicationMessageInterceptor UndeliveredMessageInterceptor { get; set; }

        MqttServerTcpEndpointOptions DefaultEndpointOptions { get; }
        MqttServerTlsTcpEndpointOptions TlsEndpointOptions { get; }

        IMqttServerStorage Storage { get; }       
        

    }
}