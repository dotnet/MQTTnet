using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client;
using MQTTnet.Packets;

namespace MQTTnet.Extensions.ManagedClient
{
    public interface IManagedMqttClient : IDisposable
    {
        event Func<ApplicationMessageProcessedEventArgs, Task> ApplicationMessageProcessedAsync;
        
        event Func<MqttApplicationMessageReceivedEventArgs, Task> ApplicationMessageReceivedAsync;
        
        event Func<EventArgs, Task> ConnectedAsync;
        
        event Func<ConnectingFailedEventArgs, Task> ConnectingFailedAsync;
        
        event Func<EventArgs, Task> ConnectionStateChangedAsync;
        
        event Func<EventArgs, Task> DisconnectedAsync;
        
        IApplicationMessageSkippedHandler ApplicationMessageSkippedHandler { get; set; }
        
        IMqttClient InternalClient { get; }
        
        bool IsConnected { get; }
        
        bool IsStarted { get; }
        
        ManagedMqttClientOptions Options { get; }
        
        int PendingApplicationMessagesCount { get; }
        
        ISynchronizingSubscriptionsFailedHandler SynchronizingSubscriptionsFailedHandler { get; set; }
        
        Task EnqueueAsync(MqttApplicationMessage applicationMessage);
        
        Task EnqueueAsync(ManagedMqttApplicationMessage applicationMessage);
        
        Task PingAsync(CancellationToken cancellationToken = default);
        
        Task StartAsync(ManagedMqttClientOptions options);
        
        Task StopAsync();
        
        Task SubscribeAsync(ICollection<MqttTopicFilter> topicFilters);
        
        Task UnsubscribeAsync(ICollection<string> topics);
    }
}