using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MQTTnet.Client
{
    public interface IMqttClient : IApplicationMessageReceiver, IApplicationMessagePublisher, IDisposable
    {
        bool IsConnected { get; }
        IMqttClientOptions Options { get; }

        event EventHandler<MqttClientConnectedEventArgs> Connected;
        event EventHandler<MqttClientDisconnectedEventArgs> Disconnected;

        Task<MqttClientConnectResult> ConnectAsync(IMqttClientOptions options);
        Task DisconnectAsync();

        Task<IList<MqttSubscribeResult>> SubscribeAsync(IEnumerable<TopicFilter> topicFilters);
        Task UnsubscribeAsync(IEnumerable<string> topics);
    }
}