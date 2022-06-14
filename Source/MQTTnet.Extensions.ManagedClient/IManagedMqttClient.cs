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
        
        event Func<ApplicationMessageSkippedEventArgs, Task> ApplicationMessageSkippedAsync;
        
        event Func<MqttClientConnectedEventArgs, Task> ConnectedAsync;
        
        event Func<ConnectingFailedEventArgs, Task> ConnectingFailedAsync;
        
        event Func<EventArgs, Task> ConnectionStateChangedAsync;
        
        event Func<MqttClientDisconnectedEventArgs, Task> DisconnectedAsync;
        
        event Func<ManagedProcessFailedEventArgs, Task> SynchronizingSubscriptionsFailedAsync;
        
        IMqttClient InternalClient { get; }
        
        bool IsConnected { get; }
        
        bool IsStarted { get; }
        
        ManagedMqttClientOptions Options { get; }
        
        int PendingApplicationMessagesCount { get; }
        
        Task EnqueueAsync(MqttApplicationMessage applicationMessage);
        
        Task EnqueueAsync(ManagedMqttApplicationMessage applicationMessage);
        
        Task PingAsync(CancellationToken cancellationToken = default);
        
        Task StartAsync(ManagedMqttClientOptions options);
        
        Task StopAsync();
        
        Task SubscribeAsync(ICollection<MqttTopicFilter> topicFilters);
        
        Task UnsubscribeAsync(ICollection<string> topics);
    }
}