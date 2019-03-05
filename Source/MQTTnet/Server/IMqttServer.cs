using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet.Server.Status;

namespace MQTTnet.Server
{
    public interface IMqttServer : IApplicationMessageReceiver, IApplicationMessagePublisher
    {
        event EventHandler Started;
        event EventHandler Stopped;

        IMqttServerClientConnectedHandler ClientConnectedHandler { get; set; }
        [Obsolete("Use ClientConnectedHandler instead.")]
        event EventHandler<MqttServerClientConnectedEventArgs> ClientConnected;

        IMqttServerClientDisconnectedHandler ClientDisconnectedHandler { get; set; }
        [Obsolete("Use ClientDisconnectedHandler instead.")]
        event EventHandler<MqttServerClientDisconnectedEventArgs> ClientDisconnected;

        event EventHandler<MqttClientSubscribedTopicEventArgs> ClientSubscribedTopic;

        event EventHandler<MqttClientUnsubscribedTopicEventArgs> ClientUnsubscribedTopic;
        
        IMqttServerOptions Options { get; }

        Task<IList<IMqttClientStatus>> GetClientStatusAsync();
        Task<IList<IMqttSessionStatus>> GetSessionStatusAsync();

        Task<IList<MqttApplicationMessage>> GetRetainedMessagesAsync();
        Task ClearRetainedMessagesAsync();

        Task SubscribeAsync(string clientId, ICollection<TopicFilter> topicFilters);
        Task UnsubscribeAsync(string clientId, ICollection<string> topicFilters);

        Task StartAsync(IMqttServerOptions options);
        Task StopAsync();
    }
}