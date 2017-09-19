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

        Task<IList<MqttSubscribeResult>> SubscribeAsync(IEnumerable<TopicFilter> topicFilters);
        Task UnsubscribeAsync(IEnumerable<string> topicFilters);

        Task PublishAsync(IEnumerable<MqttApplicationMessage> applicationMessages);
    }
}