using System;
using System.Threading.Tasks;
using MQTTnet.Client.Receiving;
using MQTTnet.Diagnostics;
using static MQTTnet.Server.MqttClientSessionsManager;

namespace MQTTnet.Server
{
    public class MqttServerEventDispatcher
    {
        private readonly IMqttNetChildLogger _logger;

        public MqttServerEventDispatcher(IMqttNetChildLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IMqttServerClientConnectedHandler ClientConnectedHandler { get; set; }

        public IMqttServerClientDisconnectedHandler ClientDisconnectedHandler { get; set; }

        public IMqttServerClientSubscribedTopicHandler ClientSubscribedTopicHandler { get; set; }

        public IMqttServerClientUnsubscribedTopicHandler ClientUnsubscribedTopicHandler { get; set; }

        public IMqttApplicationMessageReceivedHandler ApplicationMessageReceivedHandler { get; set; }

        public Task HandleClientConnectedAsync(string clientId)
        {
            var handler = ClientConnectedHandler;
            if (handler == null)
            {
                return Task.FromResult(0);
            }

            return handler.HandleClientConnectedAsync(new MqttServerClientConnectedEventArgs(clientId));
        }

        public async Task TryHandleClientDisconnectedAsync(string clientId, MqttClientDisconnectType disconnectType)
        {
            try
            {
                var handler = ClientDisconnectedHandler;
                if (handler == null)
                {
                    return;
                }

                await handler.HandleClientDisconnectedAsync(new MqttServerClientDisconnectedEventArgs(clientId, disconnectType)).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Error while handling 'ClientDisconnected' event.");
            }
        }

        public Task HandleClientSubscribedTopicAsync(string clientId, TopicFilter topicFilter)
        {
            TestLogger.WriteLine("handle sub");
            var handler = ClientSubscribedTopicHandler;
            if (handler == null)
            {
                return Task.FromResult(0);
            }

            return handler.HandleClientSubscribedTopicAsync(new MqttServerClientSubscribedTopicEventArgs(clientId, topicFilter));
        }

        public Task HandleClientUnsubscribedTopicAsync(string clientId, string topicFilter)
        {
            var handler = ClientUnsubscribedTopicHandler;
            if (handler == null)
            {
                return Task.FromResult(0);
            }

            return handler.HandleClientUnsubscribedTopicAsync(new MqttServerClientUnsubscribedTopicEventArgs(clientId, topicFilter));
        }

        public Task HandleApplicationMessageReceivedAsync(string senderClientId, MqttApplicationMessage applicationMessage)
        {
            var handler = ApplicationMessageReceivedHandler;
            if (handler == null)
            {
                return Task.FromResult(0);
            }

            return handler.HandleApplicationMessageReceivedAsync(new MqttApplicationMessageReceivedEventArgs(senderClientId, applicationMessage));
        }
    }
}
