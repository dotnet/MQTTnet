using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet.Client;

namespace MQTTnet.ManagedClient
{
    public interface IManagedMqttClient : IApplicationMessageReceiver, IApplicationMessagePublisher
    {
        bool IsConnected { get; }

        event EventHandler<MqttClientConnectedEventArgs> Connected;
        event EventHandler<MqttClientDisconnectedEventArgs> Disconnected;

        Task StartAsync(IManagedMqttClientOptions options);
        Task StopAsync();

        Task SubscribeAsync(IEnumerable<TopicFilter> topicFilters);
        Task UnsubscribeAsync(IEnumerable<string> topics);
    }
}