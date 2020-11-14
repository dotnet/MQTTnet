using System;

namespace MQTTnet.Server
{
    public class MqttServerOptions : IMqttServerOptions
    {
        public MqttServerTcpEndpointOptions DefaultEndpointOptions { get; } = new MqttServerTcpEndpointOptions();

        public MqttServerTlsTcpEndpointOptions TlsEndpointOptions { get; } = new MqttServerTlsTcpEndpointOptions();

        public string ClientId { get; set; }

        public bool EnablePersistentSessions { get; set; }

        public int MaxPendingMessagesPerClient { get; set; } = 250;

        public MqttPendingMessagesOverflowStrategy PendingMessagesOverflowStrategy { get; set; } = MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage;

        public TimeSpan DefaultCommunicationTimeout { get; set; } = TimeSpan.FromSeconds(15);

        public TimeSpan KeepAliveMonitorInterval { get; set; } = TimeSpan.FromMilliseconds(500);

        public IMqttServerConnectionValidator ConnectionValidator { get; set; }

        public IMqttServerApplicationMessageInterceptor ApplicationMessageInterceptor { get; set; }

        public IMqttServerClientMessageQueueInterceptor ClientMessageQueueInterceptor { get; set; }

        public IMqttServerSubscriptionInterceptor SubscriptionInterceptor { get; set; }

        public IMqttServerUnsubscriptionInterceptor UnsubscriptionInterceptor { get; set; }

        public IMqttServerStorage Storage { get; set; }

        public IMqttRetainedMessagesManager RetainedMessagesManager { get; set; } = new MqttRetainedMessagesManager();

        public IMqttServerApplicationMessageInterceptor UndeliveredMessageInterceptor { get; set; }

        public IMqttServerClientDisconnectedHandler ClientDisconnectedInterceptor { get; set; }
    }
}
