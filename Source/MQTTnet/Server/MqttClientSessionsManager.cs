using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Formatter;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server.Status;

namespace MQTTnet.Server
{
    public class MqttClientSessionsManager : IDisposable
    {
        private readonly AsyncQueue<MqttEnqueuedApplicationMessage> _messageQueue = new AsyncQueue<MqttEnqueuedApplicationMessage>();

        private readonly SemaphoreSlim _createConnectionGate = new SemaphoreSlim(1, 1);
        private readonly ConcurrentDictionary<string, MqttClientConnection> _connections = new ConcurrentDictionary<string, MqttClientConnection>();
        private readonly ConcurrentDictionary<string, MqttClientSession> _sessions = new ConcurrentDictionary<string, MqttClientSession>();
        private readonly IDictionary<object, object> _serverSessionItems = new ConcurrentDictionary<object, object>();

        private readonly CancellationToken _cancellationToken;
        private readonly MqttServerEventDispatcher _eventDispatcher;

        private readonly MqttRetainedMessagesManager _retainedMessagesManager;
        private readonly IMqttServerOptions _options;
        private readonly IMqttNetChildLogger _logger;

        public MqttClientSessionsManager(
            IMqttServerOptions options,
            MqttRetainedMessagesManager retainedMessagesManager,
            CancellationToken cancellationToken,
            MqttServerEventDispatcher eventDispatcher,
            IMqttNetChildLogger logger)
        {
            _cancellationToken = cancellationToken;

            if (logger == null) throw new ArgumentNullException(nameof(logger));
            _logger = logger.CreateChildLogger(nameof(MqttClientSessionsManager));

            _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _retainedMessagesManager = retainedMessagesManager ?? throw new ArgumentNullException(nameof(retainedMessagesManager));
        }

        public void Start()
        {
            Task.Run(() => TryProcessQueuedApplicationMessagesAsync(_cancellationToken), _cancellationToken).Forget(_logger);
        }

        public async Task StopAsync()
        {
            foreach (var connection in _connections.Values)
            {
                await connection.StopAsync().ConfigureAwait(false);
            }
        }

        public Task HandleClientAsync(IMqttChannelAdapter clientAdapter)
        {
            return HandleClientAsync(clientAdapter, _cancellationToken);
        }

        public Task<IList<IMqttClientStatus>> GetClientStatusAsync()
        {
            var result = new List<IMqttClientStatus>();

            foreach (var connection in _connections.Values)
            {
                var clientStatus = new MqttClientStatus(connection);
                connection.FillStatus(clientStatus);
                
                var sessionStatus = new MqttSessionStatus(connection.Session, this);
                connection.Session.FillStatus(sessionStatus);
                clientStatus.Session = sessionStatus;

                result.Add(clientStatus);
            }

            return Task.FromResult((IList<IMqttClientStatus>)result);
        }

        public Task<IList<IMqttSessionStatus>> GetSessionStatusAsync()
        {
            var result = new List<IMqttSessionStatus>();

            foreach (var session in _sessions.Values)
            {
                var sessionStatus = new MqttSessionStatus(session, this);
                session.FillStatus(sessionStatus);
    
                result.Add(sessionStatus);
            }

            return Task.FromResult((IList<IMqttSessionStatus>)result);
        }

        public void DispatchApplicationMessage(MqttApplicationMessage applicationMessage, MqttClientConnection sender)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            _messageQueue.Enqueue(new MqttEnqueuedApplicationMessage(applicationMessage, sender));
        }

        public Task SubscribeAsync(string clientId, ICollection<TopicFilter> topicFilters)
        {
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            if (!_sessions.TryGetValue(clientId, out var session))
            {
                throw new InvalidOperationException($"Client session '{clientId}' is unknown.");
            }

            return session.SubscribeAsync(topicFilters, _retainedMessagesManager);
        }

        public Task UnsubscribeAsync(string clientId, IEnumerable<string> topicFilters)
        {
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            if (!_sessions.TryGetValue(clientId, out var session))
            {
                throw new InvalidOperationException($"Client session '{clientId}' is unknown.");
            }

            return session.UnsubscribeAsync(topicFilters);
        }

        public async Task DeleteSessionAsync(string clientId)
        {
            if (_connections.TryGetValue(clientId, out var connection))
            {
                await connection.StopAsync().ConfigureAwait(false);
            }

            if (_sessions.TryRemove(clientId, out _))
            {
            }

            _logger.Verbose("Session for client '{0}' deleted.", clientId);
        }

        public void Dispose()
        {
            _messageQueue?.Dispose();
        }

        private async Task TryProcessQueuedApplicationMessagesAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await TryProcessNextQueuedApplicationMessageAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, "Unhandled exception while processing queued application messages.");
                }
            }
        }

        private async Task TryProcessNextQueuedApplicationMessageAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var dequeueResult = await _messageQueue.TryDequeueAsync(cancellationToken).ConfigureAwait(false);
                var queuedApplicationMessage = dequeueResult.Item;

                var sender = queuedApplicationMessage.Sender;
                var applicationMessage = queuedApplicationMessage.ApplicationMessage;

                var interceptorContext = await InterceptApplicationMessageAsync(sender, applicationMessage).ConfigureAwait(false);
                if (interceptorContext != null)
                {
                    if (interceptorContext.CloseConnection)
                    {
                        if (sender != null)
                        {
                            await sender.StopAsync().ConfigureAwait(false);
                        }
                    }

                    if (interceptorContext.ApplicationMessage == null || !interceptorContext.AcceptPublish)
                    {
                        return;
                    }

                    applicationMessage = interceptorContext.ApplicationMessage;
                }

                await _eventDispatcher.HandleApplicationMessageReceivedAsync(sender?.ClientId, applicationMessage).ConfigureAwait(false);

                if (applicationMessage.Retain)
                {
                    await _retainedMessagesManager.HandleMessageAsync(sender?.ClientId, applicationMessage).ConfigureAwait(false);
                }

                foreach (var clientSession in _sessions.Values)
                {
                    clientSession.EnqueueApplicationMessage(
                        applicationMessage,
                        sender?.ClientId,
                        false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Unhandled exception while processing next queued application message.");
            }
        }

        private async Task HandleClientAsync(IMqttChannelAdapter channelAdapter, CancellationToken cancellationToken)
        {
            var disconnectType = MqttClientDisconnectType.NotClean;
            string clientId = null;
            var clientWasConnected = true;

            try
            {
                var firstPacket = await channelAdapter.ReceivePacketAsync(_options.DefaultCommunicationTimeout, cancellationToken).ConfigureAwait(false);
                if (!(firstPacket is MqttConnectPacket connectPacket))
                {
                    _logger.Warning(null, "The first packet from client '{0}' was no 'CONNECT' packet [MQTT-3.1.0-1].", channelAdapter.Endpoint);
                    return;
                }

                var connectionValidatorContext = await ValidateConnectionAsync(connectPacket, channelAdapter).ConfigureAwait(false);

                clientId = connectPacket.ClientId;

                if (connectionValidatorContext.ReasonCode != MqttConnectReasonCode.Success)
                {
                    clientWasConnected = false;
                    // Send failure response here without preparing a session. The result for a successful connect
                    // will be sent from the session itself.
                    var connAckPacket = channelAdapter.PacketFormatterAdapter.DataConverter.CreateConnAckPacket(connectionValidatorContext);
                    await channelAdapter.SendPacketAsync(connAckPacket, _options.DefaultCommunicationTimeout, cancellationToken).ConfigureAwait(false);

                    return;
                }

                var connection = await CreateConnectionAsync(connectPacket, connectionValidatorContext, channelAdapter).ConfigureAwait(false);

                await _eventDispatcher.HandleClientConnectedAsync(clientId).ConfigureAwait(false);
                
                disconnectType = await connection.RunAsync(connectionValidatorContext).ConfigureAwait(false);
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
                if (clientWasConnected)
                { 
                    if (clientId != null)
                    {
                        _connections.TryRemove(clientId, out _);

                        if (!_options.EnablePersistentSessions)
                        {
                            await DeleteSessionAsync(clientId).ConfigureAwait(false);
                        }
                    }

                    await TryCleanupChannelAsync(channelAdapter).ConfigureAwait(false);

                    if (clientId != null)
                    {
                        await _eventDispatcher.TryHandleClientDisconnectedAsync(clientId, disconnectType).ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task<MqttConnectionValidatorContext> ValidateConnectionAsync(MqttConnectPacket connectPacket, IMqttChannelAdapter channelAdapter)
        {
            var context = new MqttConnectionValidatorContext(connectPacket, channelAdapter, new ConcurrentDictionary<object, object>());

            var connectionValidator = _options.ConnectionValidator;

            if (connectionValidator == null)
            {
                context.ReasonCode = MqttConnectReasonCode.Success;
                return context;
            }

            await connectionValidator.ValidateConnectionAsync(context).ConfigureAwait(false);

            // Check the client ID and set a random one if supported.
            if (string.IsNullOrEmpty(connectPacket.ClientId) && channelAdapter.PacketFormatterAdapter.ProtocolVersion == MqttProtocolVersion.V500)
            {
                connectPacket.ClientId = context.AssignedClientIdentifier;
            }

            if (string.IsNullOrEmpty(connectPacket.ClientId))
            {
                context.ReasonCode = MqttConnectReasonCode.ClientIdentifierNotValid;
            }

            return context;
        }

        private async Task<MqttClientConnection> CreateConnectionAsync(MqttConnectPacket connectPacket, MqttConnectionValidatorContext connectionValidatorContext, IMqttChannelAdapter channelAdapter)
        {
            await _createConnectionGate.WaitAsync(_cancellationToken).ConfigureAwait(false);
            try
            {
                var isSessionPresent = _sessions.TryGetValue(connectPacket.ClientId, out var session);

                var isConnectionPresent = _connections.TryGetValue(connectPacket.ClientId, out var existingConnection);
                if (isConnectionPresent)
                {
                    await existingConnection.StopAsync().ConfigureAwait(false);
                }
                
                if (isSessionPresent)
                {
                    if (connectPacket.CleanSession)
                    {
                        session = null;
                        
                        _logger.Verbose("Deleting existing session of client '{0}'.", connectPacket.ClientId);
                    }
                    else
                    {
                        _logger.Verbose("Reusing existing session of client '{0}'.", connectPacket.ClientId);
                    }
                }

                if (session == null)
                {
                    session = new MqttClientSession(connectPacket.ClientId, connectionValidatorContext.SessionItems, _eventDispatcher, _options, _logger);
                    _logger.Verbose("Created a new session for client '{0}'.", connectPacket.ClientId);
                }

                var connection = new MqttClientConnection(connectPacket, channelAdapter, session, _options, this, _retainedMessagesManager, _logger);

                _connections[connection.ClientId] = connection;
                _sessions[session.ClientId] = session;

                return connection;
            }
            finally
            {
                _createConnectionGate.Release();
            }
        }

        private async Task<MqttApplicationMessageInterceptorContext> InterceptApplicationMessageAsync(MqttClientConnection senderConnection, MqttApplicationMessage applicationMessage)
        {
            var interceptor = _options.ApplicationMessageInterceptor;
            if (interceptor == null)
            {
                return null;
            }

            string senderClientId;
            IDictionary<object, object> sessionItems;

            var messageIsFromServer = senderConnection == null;
            if (messageIsFromServer)
            {
                senderClientId = _options.ClientId;
                sessionItems = _serverSessionItems;
            }
            else
            {
                senderClientId = senderConnection.ClientId;
                sessionItems = senderConnection.Session.Items;
            }

            var interceptorContext = new MqttApplicationMessageInterceptorContext(senderClientId, sessionItems, applicationMessage);
            await interceptor.InterceptApplicationMessagePublishAsync(interceptorContext).ConfigureAwait(false);
            return interceptorContext;
        }

        private async Task TryCleanupChannelAsync(IMqttChannelAdapter channelAdapter)
        {
            try
            {
                await channelAdapter.DisconnectAsync(_options.DefaultCommunicationTimeout, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Error while disconnecting client channel.");
            }
        }
    }
}