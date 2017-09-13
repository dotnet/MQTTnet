using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet.Core.Packets;

namespace MQTTnet.Core.Client
{
    public interface IMqttClient
    {
        bool IsConnected { get; }

        event EventHandler<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;
        event EventHandler Connected;
        event EventHandler Disconnected;

        Task ConnectAsync(MqttApplicationMessage willApplicationMessage = null);
        Task DisconnectAsync();
        Task PublishAsync(IEnumerable<MqttApplicationMessage> applicationMessages);
        Task<IList<MqttSubscribeResult>> SubscribeAsync(IList<TopicFilter> topicFilters);
        Task<IList<MqttSubscribeResult>> SubscribeAsync(params TopicFilter[] topicFilters);
        Task Unsubscribe(IList<string> topicFilters);
        Task Unsubscribe(params string[] topicFilters);
    }
}