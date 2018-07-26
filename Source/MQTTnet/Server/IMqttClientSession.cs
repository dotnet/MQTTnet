using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Packets;

namespace MQTTnet.Server
{
    public interface IMqttClientSession : IDisposable
    {
        string ClientId { get; }
        void FillStatus(MqttClientSessionStatus status);

        void EnqueueApplicationMessage(MqttClientSession senderClientSession, MqttPublishPacket publishPacket);
        void ClearPendingApplicationMessages();
        
        Task RunAsync(MqttConnectPacket connectPacket, IMqttChannelAdapter adapter);
        void Stop(MqttClientDisconnectType disconnectType);

        Task SubscribeAsync(IList<TopicFilter> topicFilters);
        Task UnsubscribeAsync(IList<string> topicFilters);
    }
}