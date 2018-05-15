using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public interface IMqttServer : IApplicationMessageReceiver, IApplicationMessagePublisher
    {
        event EventHandler<MqttServerStartedEventArgs> Started;

        event EventHandler<MqttClientConnectedEventArgs> ClientConnected;
        event EventHandler<MqttClientDisconnectedEventArgs> ClientDisconnected;
        event EventHandler<MqttClientSubscribedTopicEventArgs> ClientSubscribedTopic;
        event EventHandler<MqttClientUnsubscribedTopicEventArgs> ClientUnsubscribedTopic;
        
        IMqttServerOptions Options { get; }

        Task<IList<IMqttClientSessionStatus>> GetClientSessionsStatusAsync();

        Task SubscribeAsync(string clientId, IList<TopicFilter> topicFilters);
        Task UnsubscribeAsync(string clientId, IList<string> topicFilters);

        Task StartAsync(IMqttServerOptions options);
        Task StopAsync();
    }
}