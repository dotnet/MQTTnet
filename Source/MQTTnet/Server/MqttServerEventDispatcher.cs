using MQTTnet.Client.Receiving;
using MQTTnet.Diagnostics;
using System;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public sealed class MqttServerEventDispatcher
    {
        readonly IMqttNetScopedLogger _logger;

        public MqttServerEventDispatcher(IMqttNetLogger logger)
        {
            if (logger is null) throw new ArgumentNullException(nameof(logger));

            _logger = logger.CreateScopedLogger(nameof(MqttServerEventDispatcher));
        }

        public IMqttServerClientConnectedHandler ClientConnectedHandler { get; set; }

        public IMqttServerClientDisconnectedHandler ClientDisconnectedHandler { get; set; }

        public IMqttServerClientSubscribedTopicHandler ClientSubscribedTopicHandler { get; set; }

        public IMqttServerClientUnsubscribedTopicHandler ClientUnsubscribedTopicHandler { get; set; }

        public IMqttApplicationMessageReceivedHandler ApplicationMessageReceivedHandler { get; set; }

        public async Task SafeNotifyClientConnectedAsync(string clientId)
        {
            try
            {
                var handler = ClientConnectedHandler;
                if (handler == null)
                {
                    return;
                }

                await handler.HandleClientConnectedAsync(new MqttServerClientConnectedEventArgs(clientId)).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Error while handling custom 'ClientConnected' event.");
            }
        }

        public async Task SafeNotifyClientDisconnectedAsync(string clientId, MqttClientDisconnectType disconnectType)
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
                _logger.Error(exception, "Error while handling custom 'ClientDisconnected' event.");
            }
        }

        public async Task SafeNotifyClientSubscribedTopicAsync(string clientId, MqttTopicFilter topicFilter)
        {
            try
            {
                var handler = ClientSubscribedTopicHandler;
                if (handler == null)
                {
                    return;
                }

                await handler.HandleClientSubscribedTopicAsync(new MqttServerClientSubscribedTopicEventArgs(clientId, topicFilter)).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Error while handling custom 'ClientSubscribedTopic' event.");
            }
        }

        public async Task SafeNotifyClientUnsubscribedTopicAsync(string clientId, string topicFilter)
        {
            try
            {
                var handler = ClientUnsubscribedTopicHandler;
                if (handler == null)
                {
                    return;
                }

                await handler.HandleClientUnsubscribedTopicAsync(new MqttServerClientUnsubscribedTopicEventArgs(clientId, topicFilter)).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Error while handling custom 'ClientUnsubscribedTopic' event.");
            }
        }

        public async Task SafeNotifyApplicationMessageReceivedAsync(string senderClientId, MqttApplicationMessage applicationMessage)
        {
            try
            {
                var handler = ApplicationMessageReceivedHandler;
                if (handler == null)
                {
                    return;
                }

                await handler.HandleApplicationMessageReceivedAsync(new MqttApplicationMessageReceivedEventArgs(senderClientId, applicationMessage)).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Error while handling custom 'ApplicationMessageReceived' event.");
            }
        }
    }
}
