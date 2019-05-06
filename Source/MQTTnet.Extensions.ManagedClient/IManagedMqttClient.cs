using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;

namespace MQTTnet.Extensions.ManagedClient
{
    public interface IManagedMqttClient : IApplicationMessageReceiver, IApplicationMessagePublisher, IDisposable
    {
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

        Task SubscribeAsync(IEnumerable<TopicFilter> topicFilters);
        Task UnsubscribeAsync(IEnumerable<string> topics);

        Task PublishAsync(ManagedMqttApplicationMessage applicationMessages);
    }
}