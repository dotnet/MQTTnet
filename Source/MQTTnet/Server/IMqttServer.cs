using System;
using MQTTnet.Server.Status;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public interface IMqttServer : IApplicationMessageReceiver, IApplicationMessagePublisher, IDisposable
    {
        bool IsStarted { get; }
        IMqttServerStartedHandler StartedHandler { get; set; }
        IMqttServerStoppedHandler StoppedHandler { get; set; }

        IMqttServerClientConnectedHandler ClientConnectedHandler { get; set; }
        IMqttServerClientDisconnectedHandler ClientDisconnectedHandler { get; set; }
        IMqttServerClientSubscribedTopicHandler ClientSubscribedTopicHandler { get; set; }
        IMqttServerClientUnsubscribedTopicHandler ClientUnsubscribedTopicHandler { get; set; }

        IMqttServerOptions Options { get; }

        Task<IList<IMqttClientStatus>> GetClientStatusAsync();
        Task<IList<IMqttSessionStatus>> GetSessionStatusAsync();

        Task<IList<MqttApplicationMessage>> GetRetainedApplicationMessagesAsync();
        Task ClearRetainedApplicationMessagesAsync();

        Task SubscribeAsync(string clientId, ICollection<MqttTopicFilter> topicFilters);
        Task UnsubscribeAsync(string clientId, ICollection<string> topicFilters);

        Task StartAsync(IMqttServerOptions options);
        Task StopAsync();
    }
}