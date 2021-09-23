using System;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Client.Receiving;
using MQTTnet.Diagnostics;
using MQTTnet.Packets;

namespace MQTTnet.Server.Internal
{
    public sealed class MqttServerEventDispatcher
    {
        readonly MqttNetSourceLogger _logger;

        public MqttServerEventDispatcher(IMqttNetLogger logger)
        {
            if (logger is null) throw new ArgumentNullException(nameof(logger));

            _logger = logger.WithSource(nameof(MqttServerEventDispatcher));
        }

        public IMqttServerClientConnectedHandler ClientConnectedHandler { get; set; }

        public IMqttServerClientDisconnectedHandler ClientDisconnectedHandler { get; set; }

        public IMqttServerClientSubscribedTopicHandler ClientSubscribedTopicHandler { get; set; }

        public IMqttServerClientUnsubscribedTopicHandler ClientUnsubscribedTopicHandler { get; set; }

        public IMqttApplicationMessageReceivedHandler ApplicationMessageReceivedHandler { get; set; }

        public async Task SafeNotifyClientConnectedAsync(MqttConnectPacket connectPacket, IMqttChannelAdapter channelAdapter)
        {
            try
            {
                var handler = ClientConnectedHandler;
                if (handler == null)
                {
                    return;
                }

                await handler.HandleClientConnectedAsync(new MqttServerClientConnectedEventArgs
                {
                    ClientId = connectPacket.ClientId,
                    UserName = connectPacket.Username,
                    ProtocolVersion = channelAdapter.PacketFormatterAdapter.ProtocolVersion,
                    Endpoint = channelAdapter.Endpoint
                }).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Error while handling custom 'ClientConnected' event.");
            }
        }

        public async Task SafeNotifyClientDisconnectedAsync(string clientId, MqttClientDisconnectType disconnectType, string endpoint)
        {
            try
            {
                var handler = ClientDisconnectedHandler;
                if (handler == null)
                {
                    return;
                }

                await handler.HandleClientDisconnectedAsync(new MqttServerClientDisconnectedEventArgs
                {
                    ClientId = clientId,
                    DisconnectType = disconnectType,
                    Endpoint = endpoint
                }).ConfigureAwait(false);
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

                await handler.HandleClientSubscribedTopicAsync(new MqttServerClientSubscribedTopicEventArgs
                {
                    ClientId = clientId,
                    TopicFilter = topicFilter
                }).ConfigureAwait(false);
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

                await handler.HandleClientUnsubscribedTopicAsync(new MqttServerClientUnsubscribedTopicEventArgs
                {
                    ClientId = clientId,
                    TopicFilter = topicFilter
                }).ConfigureAwait(false);
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

                await handler.HandleApplicationMessageReceivedAsync(new MqttApplicationMessageReceivedEventArgs(senderClientId, applicationMessage, null, null)).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Error while handling custom 'ApplicationMessageReceived' event.");
            }
        }
    }
}
