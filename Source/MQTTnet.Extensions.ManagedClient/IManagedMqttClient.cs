using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client;

namespace MQTTnet.Extensions.ManagedClient
{
    public interface IManagedMqttClient : IApplicationMessageReceiver, IApplicationMessagePublisher, IDisposable
    {
        /// <summary>
        /// Gets the internally used MQTT client.
        /// This property should be used with caution because manipulating the internal client might break the managed client.
        /// </summary>
        IMqttClient InternalClient { get; }

        bool IsStarted { get; }

        bool IsConnected { get; }

        int PendingApplicationMessagesCount { get; }

        IManagedMqttClientOptions Options { get; }

        IMqttClientConnectedHandler ConnectedHandler { get; set; }

        IMqttClientDisconnectedHandler DisconnectedHandler { get; set; }

        IApplicationMessageProcessedHandler ApplicationMessageProcessedHandler { get; set; }

        IApplicationMessageSkippedHandler ApplicationMessageSkippedHandler { get; set; }

        IConnectingFailedHandler ConnectingFailedHandler { get; set; }

        ISynchronizingSubscriptionsFailedHandler SynchronizingSubscriptionsFailedHandler { get; set; }

        Task StartAsync(IManagedMqttClientOptions options);

        Task StopAsync();

        Task PingAsync(CancellationToken cancellationToken);

        Task SubscribeAsync(IEnumerable<MqttTopicFilter> topicFilters);

        Task UnsubscribeAsync(IEnumerable<string> topics);

        Task PublishAsync(ManagedMqttApplicationMessage applicationMessages);
    }
}