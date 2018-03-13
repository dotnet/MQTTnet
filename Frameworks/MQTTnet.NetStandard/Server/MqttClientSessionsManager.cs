using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Serializer;

namespace MQTTnet.Server
{
    public sealed class MqttClientSessionsManager : IDisposable
    {
        private readonly Dictionary<string, MqttClientSession> _sessions = new Dictionary<string, MqttClientSession>();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private readonly IMqttServerOptions _options;
        private readonly MqttRetainedMessagesManager _retainedMessagesManager;
        private readonly IMqttNetLogger _logger;

        public MqttClientSessionsManager(IMqttServerOptions options, MqttRetainedMessagesManager retainedMessagesManager, IMqttNetLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _retainedMessagesManager = retainedMessagesManager ?? throw new ArgumentNullException(nameof(retainedMessagesManager));
        }

        public Action<ConnectedMqttClient> ClientConnectedCallback { get; set; }
        public Action<ConnectedMqttClient> ClientDisconnectedCallback { get; set; }
        public Action<string, TopicFilter> ClientSubscribedTopicCallback { get; set; }
        public Action<string, string> ClientUnsubscribedTopicCallback { get; set; }
        public Action<string, MqttApplicationMessage> ApplicationMessageReceivedCallback { get; set; }
        
        public async Task RunSessionAsync(IMqttChannelAdapter clientAdapter, CancellationToken cancellationToken)
        {
            var clientId = string.Empty;
            MqttClientSession clientSession = null;
            try
            {
                if (!(await clientAdapter.ReceivePacketAsync(_options.DefaultCommunicationTimeout, cancellationToken).ConfigureAwait(false) is MqttConnectPacket connectPacket))
                {
                    throw new MqttProtocolViolationException("The first packet from a client must be a 'CONNECT' packet [MQTT-3.1.0-1].");
                }

                clientId = connectPacket.ClientId;

                // Switch to the required protocol version before sending any response.
                clientAdapter.PacketSerializer.ProtocolVersion = connectPacket.ProtocolVersion;

                var connectReturnCode = ValidateConnection(connectPacket);
                if (connectReturnCode != MqttConnectReturnCode.ConnectionAccepted)
                {
                    await clientAdapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, cancellationToken, new MqttConnAckPacket
                    {
                        ConnectReturnCode = connectReturnCode
                    }).ConfigureAwait(false);

                    return;
                }

                var result = await GetOrCreateClientSessionAsync(connectPacket).ConfigureAwait(false);
                clientSession = result.Session;

                await clientAdapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, cancellationToken, new MqttConnAckPacket
                {
                    ConnectReturnCode = connectReturnCode,
                    IsSessionPresent = result.IsExistingSession
                }).ConfigureAwait(false);

                ClientConnectedCallback?.Invoke(new ConnectedMqttClient
                {
                    ClientId = clientId,
                    ProtocolVersion = clientAdapter.PacketSerializer.ProtocolVersion
                });

                await clientSession.RunAsync(connectPacket, clientAdapter).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.Error<MqttClientSessionsManager>(exception, exception.Message);
            }
            finally
            {
                try
                {
                    await clientAdapter.DisconnectAsync(_options.DefaultCommunicationTimeout).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // ignored
                }

                ClientDisconnectedCallback?.Invoke(new ConnectedMqttClient
                {
                    ClientId = clientId,
                    ProtocolVersion = clientAdapter.PacketSerializer.ProtocolVersion,
                    PendingApplicationMessages = clientSession?.PendingMessagesQueue.Count ?? 0
                });
            }
        }

        public async Task StopAsync()
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                foreach (var session in _sessions)
                {
                    await session.Value.StopAsync().ConfigureAwait(false);
                }

                _sessions.Clear();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<IList<ConnectedMqttClient>> GetConnectedClientsAsync()
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                return _sessions.Where(s => s.Value.IsConnected).Select(s => new ConnectedMqttClient
                {
                    ClientId = s.Value.ClientId,
                    ProtocolVersion = s.Value.ProtocolVersion ?? MqttProtocolVersion.V311,
                    LastPacketReceived = s.Value.KeepAliveMonitor.LastPacketReceived,
                    LastNonKeepAlivePacketReceived = s.Value.KeepAliveMonitor.LastNonKeepAlivePacketReceived,
                    PendingApplicationMessages = s.Value.PendingMessagesQueue.Count
                }).ToList();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task DispatchApplicationMessageAsync(MqttClientSession senderClientSession, MqttApplicationMessage applicationMessage)
        {
            try
            {
                applicationMessage = InterceptApplicationMessage(senderClientSession, applicationMessage);
                if (applicationMessage == null)
                {
                    return;
                }

                if (applicationMessage.Retain)
                {
                    await _retainedMessagesManager.HandleMessageAsync(senderClientSession?.ClientId, applicationMessage).ConfigureAwait(false);
                }

                ApplicationMessageReceivedCallback?.Invoke(senderClientSession?.ClientId, applicationMessage);
            }
            catch (Exception exception)
            {
                _logger.Error<MqttClientSessionsManager>(exception, "Error while processing application message");
            }

            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                foreach (var clientSession in _sessions.Values)
                {
                    await clientSession.EnqueueApplicationMessageAsync(applicationMessage);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task SubscribeAsync(string clientId, IList<TopicFilter> topicFilters)
        {
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!_sessions.TryGetValue(clientId, out var session))
                {
                    throw new InvalidOperationException($"Client session {clientId} is unknown.");
                }

                await session.SubscribeAsync(topicFilters);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task UnsubscribeAsync(string clientId, IList<string> topicFilters)
        {
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!_sessions.TryGetValue(clientId, out var session))
                {
                    throw new InvalidOperationException($"Client session {clientId} is unknown.");
                }

                await session.UnsubscribeAsync(topicFilters);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private MqttApplicationMessage InterceptApplicationMessage(MqttClientSession senderClientSession, MqttApplicationMessage applicationMessage)
        {
            var interceptor = _options.ApplicationMessageInterceptor;
            if (interceptor == null)
            {
                return applicationMessage;
            }

            var interceptorContext = new MqttApplicationMessageInterceptorContext(
                senderClientSession?.ClientId,
                applicationMessage);

            interceptor(interceptorContext);
            return interceptorContext.ApplicationMessage;
        }

        private MqttConnectReturnCode ValidateConnection(MqttConnectPacket connectPacket)
        {
            if (_options.ConnectionValidator == null)
            {
                return MqttConnectReturnCode.ConnectionAccepted;
            }

            var context = new MqttConnectionValidatorContext(
                connectPacket.ClientId,
                connectPacket.Username,
                connectPacket.Password,
                connectPacket.WillMessage);

            _options.ConnectionValidator(context);
            return context.ReturnCode;
        }

        private async Task<GetOrCreateClientSessionResult> GetOrCreateClientSessionAsync(MqttConnectPacket connectPacket)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                var isSessionPresent = _sessions.TryGetValue(connectPacket.ClientId, out var clientSession);
                if (isSessionPresent)
                {
                    if (connectPacket.CleanSession)
                    {
                        _sessions.Remove(connectPacket.ClientId);

                        await clientSession.StopAsync().ConfigureAwait(false);
                        clientSession.Dispose();
                        clientSession = null;

                        _logger.Trace<MqttClientSessionsManager>("Stopped existing session of client '{0}'.", connectPacket.ClientId);
                    }
                    else
                    {
                        _logger.Trace<MqttClientSessionsManager>("Reusing existing session of client '{0}'.", connectPacket.ClientId);
                    }
                }

                var isExistingSession = true;
                if (clientSession == null)
                {
                    isExistingSession = false;

                    clientSession = new MqttClientSession(connectPacket.ClientId, _options, _retainedMessagesManager, _logger)
                    {
                        ApplicationMessageReceivedCallback = DispatchApplicationMessageAsync
                    };

                    clientSession.SubscriptionsManager.TopicSubscribedCallback = ClientSubscribedTopicCallback;
                    clientSession.SubscriptionsManager.TopicUnsubscribedCallback = ClientUnsubscribedTopicCallback;

                    _sessions[connectPacket.ClientId] = clientSession;

                    _logger.Trace<MqttClientSessionsManager>("Created a new session for client '{0}'.", connectPacket.ClientId);
                }

                return new GetOrCreateClientSessionResult { IsExistingSession = isExistingSession, Session = clientSession };
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            ClientConnectedCallback = null;
            ClientDisconnectedCallback = null;
            ClientSubscribedTopicCallback = null;
            ClientUnsubscribedTopicCallback = null;
            ApplicationMessageReceivedCallback = null;
            
            _semaphore?.Dispose();
        }
    }
}