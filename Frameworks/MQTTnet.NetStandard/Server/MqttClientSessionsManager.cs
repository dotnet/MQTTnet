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

namespace MQTTnet.Server
{
    public sealed class MqttClientSessionsManager : IDisposable
    {
        private readonly Dictionary<string, MqttClientSession> _sessions = new Dictionary<string, MqttClientSession>();
        private readonly SemaphoreSlim _sessionsLock = new SemaphoreSlim(1, 1);

        private readonly MqttRetainedMessagesManager _retainedMessagesManager;
        private readonly IMqttServerOptions _options;
        private readonly IMqttNetLogger _logger;

        public MqttClientSessionsManager(IMqttServerOptions options, MqttServer server, MqttRetainedMessagesManager retainedMessagesManager, IMqttNetLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            Server = server ?? throw new ArgumentNullException(nameof(server));
            _retainedMessagesManager = retainedMessagesManager ?? throw new ArgumentNullException(nameof(retainedMessagesManager));
        }

        public MqttServer Server { get; }

        public async Task RunSessionAsync(IMqttChannelAdapter clientAdapter, CancellationToken cancellationToken)
        {
            var clientId = string.Empty;
            var wasCleanDisconnect = false;
            MqttClientSession clientSession = null;

            try
            {
                if (!(await clientAdapter.ReceivePacketAsync(_options.DefaultCommunicationTimeout, cancellationToken)
                    .ConfigureAwait(false) is MqttConnectPacket connectPacket))
                {
                    throw new MqttProtocolViolationException(
                        "The first packet from a client must be a 'CONNECT' packet [MQTT-3.1.0-1].");
                }

                clientId = connectPacket.ClientId;

                // Switch to the required protocol version before sending any response.
                clientAdapter.PacketSerializer.ProtocolVersion = connectPacket.ProtocolVersion;

                var connectReturnCode = ValidateConnection(connectPacket);
                if (connectReturnCode != MqttConnectReturnCode.ConnectionAccepted)
                {
                    await clientAdapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, new[]
                    {
                        new MqttConnAckPacket
                        {
                            ConnectReturnCode = connectReturnCode
                        }
                    }, cancellationToken).ConfigureAwait(false);

                    return;
                }

                var result = await PrepareClientSessionAsync(connectPacket).ConfigureAwait(false);
                clientSession = result.Session;

                await clientAdapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, new[]
                {
                    new MqttConnAckPacket
                    {
                        ConnectReturnCode = connectReturnCode,
                        IsSessionPresent = result.IsExistingSession
                    }
                }, cancellationToken).ConfigureAwait(false);

                Server.OnClientConnected(new ConnectedMqttClient
                {
                    ClientId = clientId,
                    ProtocolVersion = clientAdapter.PacketSerializer.ProtocolVersion
                });

                wasCleanDisconnect = await clientSession.RunAsync(connectPacket, clientAdapter).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.Error<MqttClientSessionsManager>(exception, exception.Message);
            }
            finally
            {
                try
                {
                    await clientAdapter.DisconnectAsync(_options.DefaultCommunicationTimeout, CancellationToken.None).ConfigureAwait(false);
                    clientAdapter.Dispose();
                }
                catch (Exception exception)
                {
                    _logger.Error<MqttClientSessionsManager>(exception, exception.Message);
                }

                Server.OnClientDisconnected(new ConnectedMqttClient
                {
                    ClientId = clientId,
                    ProtocolVersion = clientAdapter.PacketSerializer.ProtocolVersion,
                    PendingApplicationMessages = clientSession?.PendingMessagesQueue.Count ?? 0
                },
                wasCleanDisconnect);
            }
        }

        public async Task StopAsync()
        {
            await _sessionsLock.WaitAsync().ConfigureAwait(false);
            try
            {
                foreach (var session in _sessions)
                {
                    await session.Value.StopAsync(MqttClientDisconnectType.NotClean).ConfigureAwait(false);
                }

                _sessions.Clear();
            }
            finally
            {
                _sessionsLock.Release();
            }
        }

        public async Task<IList<ConnectedMqttClient>> GetConnectedClientsAsync()
        {
            await _sessionsLock.WaitAsync().ConfigureAwait(false);
            try
            {
                return _sessions.Where(s => s.Value.IsConnected).Select(s => new ConnectedMqttClient
                {
                    ClientId = s.Value.ClientId,
                    ProtocolVersion = s.Value.ProtocolVersion,
                    LastPacketReceived = s.Value.KeepAliveMonitor.LastPacketReceived,
                    LastNonKeepAlivePacketReceived = s.Value.KeepAliveMonitor.LastNonKeepAlivePacketReceived,
                    PendingApplicationMessages = s.Value.PendingMessagesQueue.Count
                }).ToList();
            }
            finally
            {
                _sessionsLock.Release();
            }
        }

        public void StartDispatchApplicationMessage(MqttClientSession senderClientSession, MqttApplicationMessage applicationMessage)
        {
            Task.Run(() => DispatchApplicationMessageAsync(senderClientSession, applicationMessage));
        }

        public async Task SubscribeAsync(string clientId, IList<TopicFilter> topicFilters)
        {
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            await _sessionsLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!_sessions.TryGetValue(clientId, out var session))
                {
                    throw new InvalidOperationException($"Client session '{clientId}' is unknown.");
                }

                await session.SubscribeAsync(topicFilters).ConfigureAwait(false);
            }
            finally
            {
                _sessionsLock.Release();
            }
        }

        public async Task UnsubscribeAsync(string clientId, IList<string> topicFilters)
        {
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            await _sessionsLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!_sessions.TryGetValue(clientId, out var session))
                {
                    throw new InvalidOperationException($"Client session '{clientId}' is unknown.");
                }

                await session.UnsubscribeAsync(topicFilters).ConfigureAwait(false);
            }
            finally
            {
                _sessionsLock.Release();
            }
        }

        public void Dispose()
        {
            _sessionsLock?.Dispose();
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

        private async Task<GetOrCreateClientSessionResult> PrepareClientSessionAsync(MqttConnectPacket connectPacket)
        {
            await _sessionsLock.WaitAsync().ConfigureAwait(false);
            try
            {
                var isSessionPresent = _sessions.TryGetValue(connectPacket.ClientId, out var clientSession);
                if (isSessionPresent)
                {
                    if (connectPacket.CleanSession)
                    {
                        _sessions.Remove(connectPacket.ClientId);

                        await clientSession.StopAsync(MqttClientDisconnectType.Clean).ConfigureAwait(false);
                        clientSession.Dispose();
                        clientSession = null;

                        _logger.Verbose<MqttClientSessionsManager>("Stopped existing session of client '{0}'.", connectPacket.ClientId);
                    }
                    else
                    {
                        _logger.Verbose<MqttClientSessionsManager>("Reusing existing session of client '{0}'.", connectPacket.ClientId);
                    }
                }

                var isExistingSession = true;
                if (clientSession == null)
                {
                    isExistingSession = false;

                    clientSession = new MqttClientSession(connectPacket.ClientId, _options, this, _retainedMessagesManager, _logger);
                    _sessions[connectPacket.ClientId] = clientSession;

                    _logger.Verbose<MqttClientSessionsManager>("Created a new session for client '{0}'.", connectPacket.ClientId);
                }

                return new GetOrCreateClientSessionResult { IsExistingSession = isExistingSession, Session = clientSession };
            }
            finally
            {
                _sessionsLock.Release();
            }
        }

        private async Task DispatchApplicationMessageAsync(MqttClientSession senderClientSession, MqttApplicationMessage applicationMessage)
        {
            try
            {
                var interceptorContext = InterceptApplicationMessage(senderClientSession, applicationMessage);
                if (interceptorContext.CloseConnection)
                {
                    await senderClientSession.StopAsync().ConfigureAwait(false);
                }

                if (interceptorContext.ApplicationMessage == null || !interceptorContext.AcceptPublish)
                {
                    return;
                }

                if (applicationMessage.Retain)
                {
                    await _retainedMessagesManager.HandleMessageAsync(senderClientSession?.ClientId, applicationMessage).ConfigureAwait(false);
                }

                Server.OnApplicationMessageReceived(senderClientSession?.ClientId, applicationMessage);
            }
            catch (Exception exception)
            {
                _logger.Error<MqttClientSessionsManager>(exception, "Error while processing application message");
            }

            await _sessionsLock.WaitAsync().ConfigureAwait(false);
            try
            {
                foreach (var clientSession in _sessions.Values)
                {
                    await clientSession.EnqueueApplicationMessageAsync(applicationMessage).ConfigureAwait(false);
                }
            }
            finally
            {
                _sessionsLock.Release();
            }
        }

        private MqttApplicationMessageInterceptorContext InterceptApplicationMessage(MqttClientSession senderClientSession, MqttApplicationMessage applicationMessage)
        {
            var interceptorContext = new MqttApplicationMessageInterceptorContext(
                senderClientSession?.ClientId,
                applicationMessage);

            var interceptor = _options.ApplicationMessageInterceptor;
            if (interceptor == null)
            {
                return interceptorContext;
            }

            interceptor(interceptorContext);
            return interceptorContext;
        }
    }
}