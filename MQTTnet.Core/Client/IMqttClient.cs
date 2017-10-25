using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet.Core.Packets;

namespace MQTTnet.Core.Client
{
    public interface IMqttClient : IApplicationMessageReceiver
    {
        bool IsConnected { get; }

        event EventHandler<MqttClientConnectedEventArgs> Connected;
        event EventHandler<MqttClientDisconnectedEventArgs> Disconnected;

        Task<MqttClientConnectResult> ConnectAsync(IMqttClientOptions options);
        Task DisconnectAsync();

        Task<IList<MqttSubscribeResult>> SubscribeAsync(IEnumerable<TopicFilter> topicFilters);
        Task UnsubscribeAsync(IEnumerable<string> topicFilters);

        Task PublishAsync(IEnumerable<MqttApplicationMessage> applicationMessages);
    }
}