using System;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Client.Receiving;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Internal;
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

        public AsyncEvent<MqttServerClientConnectedEventArgs> ClientConnectedEvent { get; } = new AsyncEvent<MqttServerClientConnectedEventArgs>();
        
        public AsyncEvent<MqttServerClientDisconnectedEventArgs> ClientDisconnectedEvent { get; } = new AsyncEvent<MqttServerClientDisconnectedEventArgs>();
        
        public AsyncEvent<MqttServerClientSubscribedTopicEventArgs> ClientSubscribedTopicEvent { get; } = new AsyncEvent<MqttServerClientSubscribedTopicEventArgs>();
        
        public AsyncEvent<MqttServerClientUnsubscribedTopicEventArgs> ClientUnsubscribedTopicEvent { get; } = new AsyncEvent<MqttServerClientUnsubscribedTopicEventArgs>();
        
        public AsyncEvent<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceivedEvent { get; } = new AsyncEvent<MqttApplicationMessageReceivedEventArgs>();
        
        public IMqttServerClientConnectedHandler ClientConnectedHandler { get; set; }

        public IMqttServerClientDisconnectedHandler ClientDisconnectedHandler { get; set; }

        public IMqttServerClientSubscribedTopicHandler ClientSubscribedTopicHandler { get; set; }

        public IMqttServerClientUnsubscribedTopicHandler ClientUnsubscribedTopicHandler { get; set; }

        public IMqttApplicationMessageReceivedHandler ApplicationMessageReceivedHandler { get; set; }

        public async Task SafeNotifyClientConnectedAsync(MqttConnectPacket connectPacket, IMqttChannelAdapter channelAdapter)
        {
            try
            {
                var eventArgs = new MqttServerClientConnectedEventArgs
                {
                    ClientId = connectPacket.ClientId,
                    UserName = connectPacket.Username,
                    ProtocolVersion = channelAdapter.PacketFormatterAdapter.ProtocolVersion,
                    Endpoint = channelAdapter.Endpoint
                };
                
                var handler = ClientConnectedHandler;
                if (handler != null)
                {
                    await handler.HandleClientConnectedAsync(eventArgs).ConfigureAwait(false);
                }

                await ClientConnectedEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
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
                var eventArgs = new MqttServerClientDisconnectedEventArgs
                {
                    ClientId = clientId,
                    DisconnectType = disconnectType,
                    Endpoint = endpoint
                };
                
                var handler = ClientDisconnectedHandler;
                if (handler != null)
                {
                    await handler.HandleClientDisconnectedAsync(eventArgs).ConfigureAwait(false);
                }

                await ClientDisconnectedEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
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
                var eventArgs = new MqttServerClientSubscribedTopicEventArgs
                {
                    ClientId = clientId,
                    TopicFilter = topicFilter
                };
                
                var handler = ClientSubscribedTopicHandler;
                if (handler != null)
                {
                    await handler.HandleClientSubscribedTopicAsync(eventArgs).ConfigureAwait(false);
                }

                await ClientSubscribedTopicEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
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
                var eventArgs = new MqttServerClientUnsubscribedTopicEventArgs
                {
                    ClientId = clientId,
                    TopicFilter = topicFilter
                };
                
                var handler = ClientUnsubscribedTopicHandler;
                if (handler != null)
                {
                    await handler.HandleClientUnsubscribedTopicAsync(eventArgs).ConfigureAwait(false);
                }

                await ClientUnsubscribedTopicEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
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
                var eventArgs = new MqttApplicationMessageReceivedEventArgs(senderClientId, applicationMessage, null, null);
                
                var handler = ApplicationMessageReceivedHandler;
                if (handler != null)
                {
                    await handler.HandleApplicationMessageReceivedAsync(eventArgs).ConfigureAwait(false);
                }

                await ApplicationMessageReceivedEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Error while handling custom 'ApplicationMessageReceived' event.");
            }
        }
    }
}
