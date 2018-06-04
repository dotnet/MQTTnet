using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public class MqttClientSessionsManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, MqttClientSession> _sessions = new ConcurrentDictionary<string, MqttClientSession>();
        private readonly AsyncLock _sessionPreparationLock = new AsyncLock();

        private readonly MqttRetainedMessagesManager _retainedMessagesManager;
        private readonly IMqttServerOptions _options;
        private readonly IMqttNetChildLogger _logger;

        public MqttClientSessionsManager(IMqttServerOptions options, MqttServer server, MqttRetainedMessagesManager retainedMessagesManager, IMqttNetChildLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            _logger = logger.CreateChildLogger(nameof(MqttClientSessionsManager));

            _options = options ?? throw new ArgumentNullException(nameof(options));
            Server = server ?? throw new ArgumentNullException(nameof(server));
            _retainedMessagesManager = retainedMessagesManager ?? throw new ArgumentNullException(nameof(retainedMessagesManager));
        }

        public MqttServer Server { get; }

        public async Task RunSessionAsync(IMqttChannelAdapter clientAdapter, CancellationToken cancellationToken)
        {
            var clientId = string.Empty;
            var wasCleanDisconnect = false;

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
                var clientSession = result.Session;

                await clientAdapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, new[]
                {
                    new MqttConnAckPacket
                    {
                        ConnectReturnCode = connectReturnCode,
                        IsSessionPresent = result.IsExistingSession
                    }
                }, cancellationToken).ConfigureAwait(false);

                Server.OnClientConnected(clientId);

                wasCleanDisconnect = await clientSession.RunAsync(connectPacket, clientAdapter).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.Error(exception, exception.Message);
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
                    _logger.Error(exception, exception.Message);
                }

                if (!_options.EnablePersistentSessions)
                {
                    DeleteSession(clientId);
                }

                Server.OnClientDisconnected(clientId, wasCleanDisconnect);
            }
        }

        public Task StopAsync()
        {
            foreach (var session in _sessions)
            {
                session.Value.Stop(MqttClientDisconnectType.NotClean);
            }

            _sessions.Clear();
            return Task.FromResult(0);
        }

        public Task<IList<IMqttClientSessionStatus>> GetClientStatusAsync()
        {
            var result = new List<IMqttClientSessionStatus>();
            foreach (var session in _sessions)
            {
                var status = new MqttClientSessionStatus(this, session.Value);
                session.Value.FillStatus(status);

                result.Add(status);
            }

            return Task.FromResult((IList<IMqttClientSessionStatus>)result);
        }

        public void StartDispatchApplicationMessage(MqttClientSession senderClientSession, MqttApplicationMessage applicationMessage)
        {
            Task.Run(() => DispatchApplicationMessageAsync(senderClientSession, applicationMessage));
        }

        public Task SubscribeAsync(string clientId, IList<TopicFilter> topicFilters)
        {
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            if (!_sessions.TryGetValue(clientId, out var session))
            {
                throw new InvalidOperationException($"Client session '{clientId}' is unknown.");
            }

            return session.SubscribeAsync(topicFilters);
        }

        public Task UnsubscribeAsync(string clientId, IList<string> topicFilters)
        {
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            if (!_sessions.TryGetValue(clientId, out var session))
            {
                throw new InvalidOperationException($"Client session '{clientId}' is unknown.");
            }

            return session.UnsubscribeAsync(topicFilters);
        }

        public void DeleteSession(string clientId)
        {
            _sessions.TryRemove(clientId, out _);
            _logger.Verbose("Session for client '{0}' deleted.", clientId);
        }

        public void Dispose()
        {
            _sessionPreparationLock?.Dispose();
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
            using (await _sessionPreparationLock.LockAsync(CancellationToken.None).ConfigureAwait(false))
            {
                var isSessionPresent = _sessions.TryGetValue(connectPacket.ClientId, out var clientSession);
                if (isSessionPresent)
                {
                    if (connectPacket.CleanSession)
                    {
                        _sessions.TryRemove(connectPacket.ClientId, out _);

                        clientSession.Stop(MqttClientDisconnectType.Clean);
                        clientSession.Dispose();
                        clientSession = null;

                        _logger.Verbose("Stopped existing session of client '{0}'.", connectPacket.ClientId);
                    }
                    else
                    {
                        _logger.Verbose("Reusing existing session of client '{0}'.", connectPacket.ClientId);
                    }
                }

                var isExistingSession = true;
                if (clientSession == null)
                {
                    isExistingSession = false;

                    clientSession = new MqttClientSession(connectPacket.ClientId, _options, this, _retainedMessagesManager, _logger);
                    _sessions[connectPacket.ClientId] = clientSession;

                    _logger.Verbose("Created a new session for client '{0}'.", connectPacket.ClientId);
                }

                return new GetOrCreateClientSessionResult { IsExistingSession = isExistingSession, Session = clientSession };
            }
        }

        private async Task DispatchApplicationMessageAsync(MqttClientSession senderClientSession, MqttApplicationMessage applicationMessage)
        {
            try
            {
                var interceptorContext = InterceptApplicationMessage(senderClientSession, applicationMessage);
                if (interceptorContext.CloseConnection)
                {
                    senderClientSession.Stop(MqttClientDisconnectType.NotClean);
                }

                if (interceptorContext.ApplicationMessage == null || !interceptorContext.AcceptPublish)
                {
                    return;
                }

                Server.OnApplicationMessageReceived(senderClientSession?.ClientId, applicationMessage);

                if (applicationMessage.Retain)
                {
                    await _retainedMessagesManager.HandleMessageAsync(senderClientSession?.ClientId, applicationMessage).ConfigureAwait(false);
                }

                foreach (var clientSession in _sessions.Values)
                {
                    clientSession.EnqueueApplicationMessage(senderClientSession, applicationMessage);
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Error while processing application message");
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