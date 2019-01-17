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
        event EventHandler<MqttClientConnectionValidatorEventArgs> ClientConnectionValidator;
        IMqttServerOptions Options { get; }

        Task<IList<IMqttClientSessionStatus>> GetClientSessionsStatusAsync();

        IList<MqttApplicationMessage> GetRetainedMessages();
        Task ClearRetainedMessagesAsync();

        Task SubscribeAsync(string clientId, IEnumerable<TopicFilter> topicFilters);
        Task UnsubscribeAsync(string clientId, IEnumerable<string> topicFilters);

        Task StartAsync(IMqttServerOptions options);
        Task StopAsync();
    }
}