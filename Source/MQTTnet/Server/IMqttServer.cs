using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public interface IMqttServer : IApplicationMessageReceiver, IApplicationMessagePublisher
    {
        event EventHandler Started;
        event EventHandler Stopped;

        event EventHandler<MqttClientConnectedEventArgs> ClientConnected;
        event EventHandler<MqttClientDisconnectedEventArgs> ClientDisconnected;
        event EventHandler<MqttClientSubscribedTopicEventArgs> ClientSubscribedTopic;
        event EventHandler<MqttClientUnsubscribedTopicEventArgs> ClientUnsubscribedTopic;
        
        IMqttServerOptions Options { get; }

        [Obsolete("This method is no longer async. Use the not async method.")]
        Task<IList<IMqttClientSessionStatus>> GetClientSessionsStatusAsync();

        IList<IMqttClientSessionStatus> GetClientSessionsStatus();

        IList<MqttApplicationMessage> GetRetainedMessages();
        Task ClearRetainedMessagesAsync();

        Task SubscribeAsync(string clientId, IList<TopicFilter> topicFilters);
        Task UnsubscribeAsync(string clientId, IList<string> topicFilters);

        Task StartAsync(IMqttServerOptions options);
        Task StopAsync();
    }
}