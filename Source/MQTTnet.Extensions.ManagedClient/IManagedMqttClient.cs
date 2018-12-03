using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet.Client;

namespace MQTTnet.Extensions.ManagedClient
{
    public interface IManagedMqttClient : IApplicationMessageReceiver, IApplicationMessagePublisher, IDisposable
    {
        bool IsStarted { get; }
        bool IsConnected { get; }
        int PendingApplicationMessagesCount { get; }
        IManagedMqttClientOptions Options { get; }

        event EventHandler<MqttClientConnectedEventArgs> Connected;
        event EventHandler<MqttClientDisconnectedEventArgs> Disconnected;

        event EventHandler<ApplicationMessageProcessedEventArgs> ApplicationMessageProcessed;
        event EventHandler<ApplicationMessageSkippedEventArgs> ApplicationMessageSkipped;

        event EventHandler<MqttManagedProcessFailedEventArgs> ConnectingFailed;
        event EventHandler<MqttManagedProcessFailedEventArgs> SynchronizingSubscriptionsFailed;
        
        Task StartAsync(IManagedMqttClientOptions options);
        Task StopAsync();

        Task SubscribeAsync(IEnumerable<TopicFilter> topicFilters);
        Task UnsubscribeAsync(IEnumerable<string> topics);

        Task PublishAsync(ManagedMqttApplicationMessage applicationMessages);
    }
}