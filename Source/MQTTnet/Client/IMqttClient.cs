using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Subscribing;
using MQTTnet.Client.Unsubscribing;

namespace MQTTnet.Client
{
    public interface IMqttClient : IApplicationMessageReceiver, IApplicationMessagePublisher, IDisposable
    {
        bool IsConnected { get; }
        IMqttClientOptions Options { get; }

        event EventHandler<MqttClientConnectedEventArgs> Connected;
        event EventHandler<MqttClientDisconnectedEventArgs> Disconnected;

        Task<MqttClientConnectResult> ConnectAsync(IMqttClientOptions options);
        Task DisconnectAsync(MqttClientDisconnectOptions options);

        Task<MqttClientSubscribeResult> SubscribeAsync(IEnumerable<TopicFilter> topicFilters);
        Task<MqttClientUnsubscribeResult> UnsubscribeAsync(IEnumerable<string> topics);
    }
}