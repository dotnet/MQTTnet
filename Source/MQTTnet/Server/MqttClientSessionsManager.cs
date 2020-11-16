using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server.Status;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OperationCanceledException = System.OperationCanceledException;

namespace MQTTnet.Server
{
    public sealed class MqttClientSessionsManager : IDisposable
    {
        readonly BlockingCollection<MqttEnqueuedApplicationMessage> _messageQueue = new BlockingCollection<MqttEnqueuedApplicationMessage>();

        readonly object _createConnectionSyncRoot = new object();
        readonly Dictionary<string, MqttClientConnection> _connections = new Dictionary<string, MqttClientConnection>();
        readonly Dictionary<string, MqttClientSession> _sessions = new Dictionary<string, MqttClientSession>();

        readonly IDictionary<object, object> _serverSessionItems = new ConcurrentDictionary<object, object>();

        readonly MqttServerEventDispatcher _eventDispatcher;

        readonly IMqttRetainedMessagesManager _retainedMessagesManager;
        readonly IMqttServerOptions _options;
        readonly IMqttNetScopedLogger _logger;
        readonly IMqttNetLogger _rootLogger;

        public MqttClientSessionsManager(
            IMqttServerOptions options,
            IMqttRetainedMessagesManager retainedMessagesManager,
            MqttServerEventDispatcher eventDispatcher,
            IMqttNetLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            _logger = logger.CreateScopedLogger(nameof(MqttClientSessionsManager));
            _rootLogger = logger;

            _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _retainedMessagesManager = retainedMessagesManager ?? throw new ArgumentNullException(nameof(retainedMessagesManager));
        }

        public void Start(CancellationToken cancellationToken)
        {
            Task.Run(() => TryProcessQueuedApplicationMessagesAsync(cancellationToken), cancellationToken).Forget(_logger);
        }

        public async Task HandleClientConnectionAsync(IMqttChannelAdapter channelAdapter, CancellationToken cancellationToken)
        {
            try
            {
                MqttConnectPacket connectPacket;
                try
                {
                    using (var timeoutToken = new CancellationTokenSource(_options.DefaultCommunicationTimeout))
                    {
                        var firstPacket = await channelAdapter.ReceivePacketAsync(timeoutToken.Token).ConfigureAwait(false);
                        connectPacket = firstPacket as MqttConnectPacket;
                        if (connectPacket == null)
                        {
                            _logger.Warning(null,
                                "Client '{0}': First received packet was no 'CONNECT' packet [MQTT-3.1.0-1].",
                                channelAdapter.Endpoint);

                            return;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.Warning(null, "Client '{0}': Connected but did not sent a CONNECT packet.", channelAdapter.Endpoint);
                    return;
                }
                catch (MqttCommunicationTimedOutException)
                {
                    _logger.Warning(null, "Client '{0}': Connected but did not sent a CONNECT packet.", channelAdapter.Endpoint);
                    return;
                }

                var connectionValidatorContext = await ValidateConnectionAsync(connectPacket, channelAdapter).ConfigureAwait(false);

                if (connectionValidatorContext.ReasonCode != MqttConnectReasonCode.Success)
                {
                    // Send failure response here without preparing a session. The result for a successful connect
                    // will be sent from the session itself.
                    var connAckPacket = channelAdapter.PacketFormatterAdapter.DataConverter.CreateConnAckPacket(connectionValidatorContext);
                    await channelAdapter.SendPacketAsync(connAckPacket, _options.DefaultCommunicationTimeout, cancellationToken).ConfigureAwait(false);

                    return;
                }

                var connection = CreateClientConnection(connectPacket, connectionValidatorContext, channelAdapter);
                await _eventDispatcher.SafeNotifyClientConnectedAsync(connectPacket.ClientId).ConfigureAwait(false);
                await connection.RunAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.Error(exception, exception.Message);
            }
        }

        public async Task CloseAllConnectionsAsync()
        {
            List<MqttClientConnection> connections;
            lock (_connections)
            {
                connections = _connections.Values.ToList();
                _connections.Clear();
            }

            foreach (var connection in connections)
            {
                await connection.StopAsync(MqttDisconnectReasonCode.NormalDisconnection).ConfigureAwait(false);
            }
        }

        public List<MqttClientConnection> GetConnections()
        {
            lock (_connections)
            {
                return _connections.Values.ToList();
            }
        }
        
        public Task<IList<IMqttClientStatus>> GetClientStatusAsync()
        {
            var result = new List<IMqttClientStatus>();

            lock (_connections)
            {
                foreach (var connection in _connections.Values)
                {
                    var clientStatus = new MqttClientStatus(connection);
                    connection.FillStatus(clientStatus);

                    var sessionStatus = new MqttSessionStatus(connection.Session, this);
                    connection.Session.FillStatus(sessionStatus);
                    clientStatus.Session = sessionStatus;

                    result.Add(clientStatus);
                }
            }

            return Task.FromResult((IList<IMqttClientStatus>)result);
        }

        public Task<IList<IMqttSessionStatus>> GetSessionStatusAsync()
        {
            var result = new List<IMqttSessionStatus>();

            lock (_sessions)
            {
                foreach (var session in _sessions.Values)
                {
                    var sessionStatus = new MqttSessionStatus(session, this);
                    session.FillStatus(sessionStatus);

                    result.Add(sessionStatus);
                }
            }

            return Task.FromResult((IList<IMqttSessionStatus>)result);
        }

        public void DispatchApplicationMessage(MqttApplicationMessage applicationMessage, MqttClientConnection sender)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            _messageQueue.Add(new MqttEnqueuedApplicationMessage(applicationMessage, sender));
        }

        public Task SubscribeAsync(string clientId, ICollection<MqttTopicFilter> topicFilters)
        {
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            return GetSession(clientId).SubscribeAsync(topicFilters);
        }

        public Task UnsubscribeAsync(string clientId, IEnumerable<string> topicFilters)
        {
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            return GetSession(clientId).UnsubscribeAsync(topicFilters);
        }

        public async Task DeleteSessionAsync(string clientId)
        {
            MqttClientConnection connection;
            lock (_connections)
            {
                _connections.TryGetValue(clientId, out connection);
            }
            
            lock (_sessions)
            {
                _sessions.Remove(clientId);
            }

            if (connection != null)
            {
                await connection.StopAsync(MqttDisconnectReasonCode.NormalDisconnection).ConfigureAwait(false);
            }
            
            _logger.Verbose("Session for client '{0}' deleted.", clientId);
        }

        public async Task CleanUpClient(string clientId, IMqttChannelAdapter channelAdapter, MqttClientDisconnectType disconnectType)
        {
            if (clientId != null)
            {
                // in case it is a takeover _connections already contains the new connection
                if (disconnectType != MqttClientDisconnectType.Takeover)
                {
                    lock (_connections)
                    {
                        _connections.Remove(clientId);
                    }

                    if (!_options.EnablePersistentSessions)
                    {
                        await DeleteSessionAsync(clientId).ConfigureAwait(false);
                    }
                }
            }

            await SafeCleanupChannelAsync(channelAdapter).ConfigureAwait(false);

            if (clientId != null)
            {
                await _eventDispatcher.SafeNotifyClientDisconnectedAsync(clientId, disconnectType).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            _messageQueue?.Dispose();
        }

        async Task TryProcessQueuedApplicationMessagesAsync(CancellationToken cancellationToken)
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

        async Task TryProcessNextQueuedApplicationMessageAsync(CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                MqttEnqueuedApplicationMessage queuedApplicationMessage;
                try
                {
                     queuedApplicationMessage = _messageQueue.Take(cancellationToken);
                }
                catch (ArgumentNullException)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }

                var sender = queuedApplicationMessage.Sender;
                var senderClientId = sender?.ClientId ?? _options.ClientId;
                var applicationMessage = queuedApplicationMessage.ApplicationMessage;

                var interceptor = _options.ApplicationMessageInterceptor;
                if (interceptor != null)
                {
                    var interceptorContext = await InterceptApplicationMessageAsync(interceptor, sender, applicationMessage).ConfigureAwait(false);
                    if (interceptorContext != null)
                    {
                        if (interceptorContext.CloseConnection)
                        {
                            if (sender != null)
                            {
                                await sender.StopAsync(MqttDisconnectReasonCode.NormalDisconnection).ConfigureAwait(false);
                            }
                        }

                        if (interceptorContext.ApplicationMessage == null || !interceptorContext.AcceptPublish)
                        {
                            return;
                        }

                        applicationMessage = interceptorContext.ApplicationMessage;
                    }
                }
                
                await _eventDispatcher.SafeNotifyApplicationMessageReceivedAsync(senderClientId, applicationMessage).ConfigureAwait(false);

                if (applicationMessage.Retain)
                {
                    await _retainedMessagesManager.HandleMessageAsync(senderClientId, applicationMessage).ConfigureAwait(false);
                }

                var deliveryCount = 0;

                lock (_sessions)
                {
                    foreach (var clientSession in _sessions.Values)
                    {
                        var isSubscribed = clientSession.EnqueueApplicationMessage(applicationMessage, senderClientId, false);
                        if (isSubscribed)
                        {
                            deliveryCount++;
                        }
                    }
                }
                
                if (deliveryCount == 0)
                {
                    var undeliveredMessageInterceptor = _options.UndeliveredMessageInterceptor;
                    if (undeliveredMessageInterceptor == null)
                    {
                        return;
                    }

                    await undeliveredMessageInterceptor.InterceptApplicationMessagePublishAsync(new MqttApplicationMessageInterceptorContext(senderClientId, sender?.Session?.Items, applicationMessage));
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

        async Task<MqttConnectionValidatorContext> ValidateConnectionAsync(MqttConnectPacket connectPacket, IMqttChannelAdapter channelAdapter)
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

        MqttClientConnection CreateClientConnection(MqttConnectPacket connectPacket, MqttConnectionValidatorContext connectionValidatorContext, IMqttChannelAdapter channelAdapter)
        {
            lock (_createConnectionSyncRoot)
            {
                MqttClientSession session;
                lock (_sessions)
                {
                    if (!_sessions.TryGetValue(connectPacket.ClientId, out session))
                    {
                        _logger.Verbose("Created a new session for client '{0}'.", connectPacket.ClientId);
                        session = CreateSession(connectPacket.ClientId, connectionValidatorContext);
                    }
                    else
                    {
                        if (connectPacket.CleanSession)
                        {
                            _logger.Verbose("Deleting existing session of client '{0}'.", connectPacket.ClientId);
                            session = CreateSession(connectPacket.ClientId, connectionValidatorContext);
                        }
                        else
                        {
                            _logger.Verbose("Reusing existing session of client '{0}'.", connectPacket.ClientId);
                        }
                    }

                    _sessions[connectPacket.ClientId] = session;
                }

                MqttClientConnection existingConnection;
                MqttClientConnection connection;
                lock (_connections)
                {
                    _connections.TryGetValue(connectPacket.ClientId, out existingConnection);
                    connection = CreateConnection(connectPacket, channelAdapter, session, connectionValidatorContext);

                    _connections[connectPacket.ClientId] = connection;
                }

                existingConnection?.StopAsync(MqttDisconnectReasonCode.SessionTakenOver).GetAwaiter().GetResult();

                return connection;
            }
        }

        async Task<MqttApplicationMessageInterceptorContext> InterceptApplicationMessageAsync(IMqttServerApplicationMessageInterceptor interceptor, MqttClientConnection senderConnection, MqttApplicationMessage applicationMessage)
        {
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

        async Task SafeCleanupChannelAsync(IMqttChannelAdapter channelAdapter)
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

        MqttClientSession GetSession(string clientId)
        {
            lock (_sessions)
            {
                if (!_sessions.TryGetValue(clientId, out var session))
                {
                    throw new InvalidOperationException($"Client session '{clientId}' is unknown.");
                }

                return session;
            }
        }

        MqttClientConnection CreateConnection(MqttConnectPacket connectPacket, IMqttChannelAdapter channelAdapter, MqttClientSession session, MqttConnectionValidatorContext connectionValidatorContext)
        {
            return new MqttClientConnection(
                connectPacket,
                channelAdapter,
                session,
                connectionValidatorContext,
                _options,
                this,
                _retainedMessagesManager,
                _rootLogger);
        }

        MqttClientSession CreateSession(string clientId, MqttConnectionValidatorContext connectionValidatorContext)
        {
            return new MqttClientSession(
                clientId,
                connectionValidatorContext.SessionItems,
                _eventDispatcher,
                _options,
                _retainedMessagesManager,
                _rootLogger);
        }
    }
}