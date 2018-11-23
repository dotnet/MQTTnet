using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet.Adapter;

namespace MQTTnet.Server
{
    public interface IMqttClientSession : IDisposable
    {
        string ClientId { get; }
        void FillStatus(MqttClientSessionStatus status);

        void EnqueueApplicationMessage(MqttClientSession senderClientSession, MqttApplicationMessage applicationMessage);
        void ClearPendingApplicationMessages();
        
        Task RunAsync(MqttApplicationMessage willMessage, int keepAliveInterval, IMqttChannelAdapter adapter);
        void Stop(MqttClientDisconnectType disconnectType);

        Task SubscribeAsync(IList<TopicFilter> topicFilters);
        Task UnsubscribeAsync(IList<string> topicFilters);
    }
}