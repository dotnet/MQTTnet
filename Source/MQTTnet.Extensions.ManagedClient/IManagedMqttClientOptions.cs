using System;
using MQTTnet.Client;
using MQTTnet.Server;

namespace MQTTnet.Extensions.ManagedClient
{
    public interface IManagedMqttClientOptions
    {
        IMqttClientOptions ClientOptions { get; }

        TimeSpan AutoReconnectDelay { get; }

        TimeSpan ConnectionCheckInterval { get; }

        IManagedMqttClientStorage Storage { get; }

        int MaxPendingMessages { get; }

        MqttPendingMessagesOverflowStrategy PendingMessagesOverflowStrategy { get; }

        /// <summary>
        /// Defines the maximum amount of topic filters which will be sent in a SUBSCRIBE/UNSUBSCRIBE packet.
        /// Amazon AWS limits this number to 8. The default is int.MaxValue.
        /// </summary>
        int MaxTopicFiltersInSubscribeUnsubscribePackets { get; }
    }
}