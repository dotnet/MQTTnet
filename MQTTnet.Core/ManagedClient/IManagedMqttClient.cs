using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet.Core.Client;

namespace MQTTnet.Core.ManagedClient
{
    public interface IManagedMqttClient : IApplicationMessageReceiver, IApplicationMessagePublisher
    {
        bool IsConnected { get; }

        event EventHandler<MqttClientConnectedEventArgs> Connected;
        event EventHandler<MqttClientDisconnectedEventArgs> Disconnected;

        Task StartAsync(IManagedMqttClientOptions options);
        Task StopAsync();

        Task SubscribeAsync(IEnumerable<TopicFilter> topicFilters);
        Task UnsubscribeAsync(IEnumerable<TopicFilter> topicFilters);
    }
}