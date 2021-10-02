using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Diagnostics;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server.Status;

namespace MQTTnet.Server.Internal
{
    public sealed class MqttClientSessionsManager : IDisposable
    {
        readonly BlockingCollection<MqttPendingApplicationMessage> _messageQueue = new BlockingCollection<MqttPendingApplicationMessage>();

        readonly AsyncLock _createConnectionSyncRoot = new AsyncLock();
        readonly Dictionary<string, MqttClientConnection> _clientConnections = new Dictionary<string, MqttClientConnection>(4096);
        readonly Dictionary<string, MqttClientSession> _clientSessions = new Dictionary<string, MqttClientSession>(4096);

        readonly IDictionary<object, object> _serverSessionItems = new ConcurrentDictionary<object, object>();

        readonly MqttServerEventDispatcher _eventDispatcher;

        readonly IMqttRetainedMessagesManager _retainedMessagesManager;
        readonly IMqttServerOptions _options;
        readonly MqttNetSourceLogger _logger;
        readonly IMqttNetLogger _rootLogger;

        public MqttClientSessionsManager(
            IMqttServerOptions options,
            IMqttRetainedMessagesManager retainedMessagesManager,
            MqttServerEventDispatcher eventDispatcher,
            IMqttNetLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            _logger = logger.WithSource(nameof(MqttClientSessionsManager));
            _rootLogger = logger;

            _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _retainedMessagesManager = retainedMessagesManager ?? throw new ArgumentNullException(nameof(retainedMessagesManager));
        }

        public void Start(CancellationToken cancellationToken)
        {
            Task.Run(() => TryProcessQueuedApplicationMessagesAsync(cancellationToken), cancellationToken).RunInBackground(_logger);
        }

        async Task<MqttConnectPacket> ReceiveConnectPacket(IMqttChannelAdapter channelAdapter, CancellationToken cancellationToken)
        {
            try
            {
                using (var timeoutToken = new CancellationTokenSource(_options.DefaultCommunicationTimeout))
                using (var effectiveCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(timeoutToken.Token, cancellationToken))
                {
                    var firstPacket = await channelAdapter.ReceivePacketAsync(effectiveCancellationToken.Token).ConfigureAwait(false);

                    if (firstPacket is MqttConnectPacket connectPacket)
                    {
                        return connectPacket;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Warning("Client '{0}': Connected but did not sent a CONNECT packet.", channelAdapter.Endpoint);
            }
            catch (MqttCommunicationTimedOutException)
            {
                _logger.Warning("Client '{0}': Connected but did not sent a CONNECT packet.", channelAdapter.Endpoint);
            }

            _logger.Warning("Client '{0}': First received packet was no 'CONNECT' packet [MQTT-3.1.0-1].", channelAdapter.Endpoint);
            return null;
        }
        
        public async Task HandleClientConnectionAsync(IMqttChannelAdapter channelAdapter, CancellationToken cancellationToken)
        {
            MqttClientConnection clientConnection = null;

            try
            {
                var connectPacket = await ReceiveConnectPacket(channelAdapter, cancellationToken).ConfigureAwait(false);
                if (connectPacket == null)
                {
                    // Nothing was received in time etc.
                    return;
                }

                MqttConnAckPacket connAckPacket;
                
                var connectionValidatorContext = await ValidateConnection(connectPacket, channelAdapter).ConfigureAwait(false);
                if (connectionValidatorContext.ReasonCode != MqttConnectReasonCode.Success)
                {
                    // Send failure response here without preparing a session!
                    connAckPacket = channelAdapter.PacketFormatterAdapter.DataConverter.CreateConnAckPacket(connectionValidatorContext);
                    await channelAdapter.SendPacketAsync(connAckPacket, cancellationToken).ConfigureAwait(false);
                    return;
                }

                clientConnection = await CreateClientConnection(connectPacket, channelAdapter, connectionValidatorContext.SessionItems).ConfigureAwait(false);

                connAckPacket = channelAdapter.PacketFormatterAdapter.DataConverter.CreateConnAckPacket(connectionValidatorContext);
                await channelAdapter.SendPacketAsync(connAckPacket, cancellationToken).ConfigureAwait(false);

                await _eventDispatcher.SafeNotifyClientConnectedAsync(connectPacket, channelAdapter).ConfigureAwait(false);

                await clientConnection.RunAsync().ConfigureAwait(false);
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
                if (clientConnection != null)
                {
                    if (clientConnection.ClientId != null)
                    {
                        // in case it is a takeover _clientConnections already contains the new connection
                        if (!clientConnection.IsTakenOver)
                        {
                            lock (_clientConnections)
                            {
                                _clientConnections.Remove(clientConnection.ClientId);
                            }

                            if (!_options.EnablePersistentSessions)
                            {
                                await DeleteSessionAsync(clientConnection.ClientId).ConfigureAwait(false);
                            }
                        }
                    }

                    var endpoint = clientConnection.Endpoint;

                    if (clientConnection.ClientId != null && !clientConnection.IsTakenOver)
                    {
                        // The event is fired at a separate place in case of a handover!
                        await _eventDispatcher.SafeNotifyClientDisconnectedAsync(
                            clientConnection.ClientId,
                            clientConnection.IsCleanDisconnect ? MqttClientDisconnectType.Clean : MqttClientDisconnectType.NotClean,
                            endpoint).ConfigureAwait(false);
                    }
                }

                await channelAdapter.DisconnectAsync(_options.DefaultCommunicationTimeout, CancellationToken.None).ConfigureAwait(false);
            }
        }

        public async Task CloseAllConnectionsAsync()
        {
            List<MqttClientConnection> connections;
            lock (_clientConnections)
            {
                connections = _clientConnections.Values.ToList();
                _clientConnections.Clear();
            }

            foreach (var connection in connections)
            {
                await connection.StopAsync(MqttClientDisconnectReason.NormalDisconnection).ConfigureAwait(false);
            }
        }

        public List<MqttClientConnection> GetConnections()
        {
            lock (_clientConnections)
            {
                return _clientConnections.Values.ToList();
            }
        }

        public Task<IList<IMqttClientStatus>> GetClientStatusAsync()
        {
            var result = new List<IMqttClientStatus>();

            lock (_clientConnections)
            {
                foreach (var connection in _clientConnections.Values)
                {
                    var clientStatus = new MqttClientStatus(connection);
                    connection.FillClientStatus(clientStatus);

                    var sessionStatus = new MqttSessionStatus(connection.Session, this);
                    connection.Session.FillSessionStatus(sessionStatus);
                    clientStatus.Session = sessionStatus;

                    result.Add(clientStatus);
                }
            }

            return Task.FromResult((IList<IMqttClientStatus>)result);
        }

        public Task<IList<IMqttSessionStatus>> GetSessionStatusAsync()
        {
            var result = new List<IMqttSessionStatus>();

            lock (_clientSessions)
            {
                foreach (var session in _clientSessions.Values)
                {
                    var sessionStatus = new MqttSessionStatus(session, this);
                    session.FillSessionStatus(sessionStatus);

                    result.Add(sessionStatus);
                }
            }

            return Task.FromResult((IList<IMqttSessionStatus>)result);
        }

        public void DispatchApplicationMessage(MqttApplicationMessage applicationMessage, MqttClientConnection sender)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            _messageQueue.Add(new MqttPendingApplicationMessage(applicationMessage, sender));
        }

        public async Task SubscribeAsync(string clientId, ICollection<MqttTopicFilter> topicFilters)
        {
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            var fakeSubscribePacket = new MqttSubscribePacket
            {
                TopicFilters = topicFilters.ToList()
            };

            var clientSession = GetClientSession(clientId);

            var subscribeResult = await clientSession.SubscriptionsManager.Subscribe(fakeSubscribePacket).ConfigureAwait(false);
            
            foreach (var retainedApplicationMessage in subscribeResult.RetainedApplicationMessages)
            {
                clientSession.ApplicationMessagesQueue.Enqueue(retainedApplicationMessage);
            }
        }

        public Task UnsubscribeAsync(string clientId, ICollection<string> topicFilters)
        {
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            var fakeUnsubscribePacket = new MqttUnsubscribePacket
            {
                TopicFilters = topicFilters.ToList()
            };
            
            return GetClientSession(clientId).SubscriptionsManager.Unsubscribe(fakeUnsubscribePacket);
        }

        public async Task DeleteSessionAsync(string clientId)
        {
            MqttClientConnection connection;
            lock (_clientConnections)
            {
                _clientConnections.TryGetValue(clientId, out connection);
            }

            lock (_clientSessions)
            {
                _clientSessions.Remove(clientId);
            }

            if (connection != null)
            {
                await connection.StopAsync(MqttClientDisconnectReason.NormalDisconnection).ConfigureAwait(false);
            }

            _logger.Verbose("Session for client '{0}' deleted.", clientId);
        }

        public void Dispose()
        {
            _messageQueue?.Dispose();
        }

        async Task TryProcessQueuedApplicationMessagesAsync(CancellationToken cancellationToken)
        {
            // Make sure all queued messages are proccessed befor server stops.
            while (!cancellationToken.IsCancellationRequested || _messageQueue.Any())
            {
                try
                {
                    await TryProcessNextQueuedApplicationMessage().ConfigureAwait(false);
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

        async Task TryProcessNextQueuedApplicationMessage()
        {
            try
            {
                MqttPendingApplicationMessage pendingApplicationMessage;
                try
                {
                    pendingApplicationMessage = _messageQueue.Take();
                }
                catch (ArgumentNullException)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }

                var clientConnection = pendingApplicationMessage.Sender;
                var senderClientId = clientConnection?.ClientId ?? _options.ClientId;
                var applicationMessage = pendingApplicationMessage.ApplicationMessage;

                var interceptor = _options.ApplicationMessageInterceptor;
                if (interceptor != null)
                {
                    var interceptorContext = await InterceptApplicationMessageAsync(interceptor, clientConnection, applicationMessage).ConfigureAwait(false);
                    if (interceptorContext != null)
                    {
                        if (interceptorContext.CloseConnection)
                        {
                            if (clientConnection != null)
                            {
                                await clientConnection.StopAsync(MqttClientDisconnectReason.NormalDisconnection).ConfigureAwait(false);
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
                List<MqttClientSession> sessions;
                lock (_clientSessions)
                {
                    sessions = _clientSessions.Values.ToList();
                }

                foreach (var clientSession in sessions)
                {
                    var checkSubscriptionsResult = clientSession.SubscriptionsManager.CheckSubscriptions(
                        applicationMessage.Topic, 
                        applicationMessage.QualityOfServiceLevel,
                        senderClientId);
                    
                    if (!checkSubscriptionsResult.IsSubscribed)
                    {
                        continue;
                    }

                    _logger.Verbose("Client '{0}': Queued application message with topic '{1}'.", clientSession.ClientId, applicationMessage.Topic);

                    var queuedApplicationMessage = new MqttQueuedApplicationMessage
                    {
                        ApplicationMessage = applicationMessage,
                        SubscriptionQualityOfServiceLevel = checkSubscriptionsResult.QualityOfServiceLevel,
                        SubscriptionIdentifiers = checkSubscriptionsResult.SubscriptionIdentifiers
                    };
                    
                    if (checkSubscriptionsResult.RetainAsPublished)
                    {
                        // Transfer the original retain state from the publisher.
                        // This is a MQTTv5 feature.
                        queuedApplicationMessage.IsRetainedMessage = applicationMessage.Retain;
                    }

                    clientSession.ApplicationMessagesQueue.Enqueue(queuedApplicationMessage);
                    deliveryCount++;
                }

                if (deliveryCount == 0)
                {
                    var undeliveredMessageInterceptor = _options.UndeliveredMessageInterceptor;
                    if (undeliveredMessageInterceptor == null)
                    {
                        return;
                    }

                    // The delegate signature is the same as for regular message interceptor. So the call is fine and just uses a different interceptor.
                    await InterceptApplicationMessageAsync(undeliveredMessageInterceptor, clientConnection, applicationMessage).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Unhandled exception while processing next queued application message.");
            }
        }

        async Task<MqttConnectionValidatorContext> ValidateConnection(MqttConnectPacket connectPacket, IMqttChannelAdapter channelAdapter)
        {
            var context = new MqttConnectionValidatorContext(connectPacket, channelAdapter)
            {
                SessionItems = new ConcurrentDictionary<object, object>()
            };

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

        async Task<MqttClientConnection> CreateClientConnection(
            MqttConnectPacket connectPacket, 
            IMqttChannelAdapter channelAdapter, 
            IDictionary<object, object> sessionItems)
        {
            MqttClientConnection connection;

            using (await _createConnectionSyncRoot.WaitAsync(CancellationToken.None).ConfigureAwait(false))
            {
                MqttClientSession session;
                lock (_clientSessions)
                {
                    if (!_clientSessions.TryGetValue(connectPacket.ClientId, out session))
                    {
                        _logger.Verbose("Created a new session for client '{0}'.", connectPacket.ClientId);
                        session = CreateSession(connectPacket.ClientId, sessionItems);
                    }
                    else
                    {
                        if (connectPacket.CleanSession)
                        {
                            _logger.Verbose("Deleting existing session of client '{0}'.", connectPacket.ClientId);
                            session = CreateSession(connectPacket.ClientId, sessionItems);
                        }
                        else
                        {
                            _logger.Verbose("Reusing existing session of client '{0}'.", connectPacket.ClientId);
                        }
                    }

                    _clientSessions[connectPacket.ClientId] = session;
                }

                MqttClientConnection existingConnection;

                lock (_clientConnections)
                {
                    _clientConnections.TryGetValue(connectPacket.ClientId, out existingConnection);
                    connection = CreateConnection(connectPacket, channelAdapter, session);

                    _clientConnections[connectPacket.ClientId] = connection;
                }

                if (existingConnection != null)
                {
                    await _eventDispatcher.SafeNotifyClientDisconnectedAsync(existingConnection.ClientId, MqttClientDisconnectType.Takeover, existingConnection.Endpoint);

                    existingConnection.IsTakenOver = true;
                    await existingConnection.StopAsync(MqttClientDisconnectReason.SessionTakenOver).ConfigureAwait(false);
                }
            }

            return connection;
        }

        async Task<MqttApplicationMessageInterceptorContext> InterceptApplicationMessageAsync(
            IMqttServerApplicationMessageInterceptor interceptor,
            MqttClientConnection clientConnection,
            MqttApplicationMessage applicationMessage)
        {
            string senderClientId;
            IDictionary<object, object> sessionItems;

            var messageIsFromServer = clientConnection == null;
            if (messageIsFromServer)
            {
                senderClientId = _options.ClientId;
                sessionItems = _serverSessionItems;
            }
            else
            {
                senderClientId = clientConnection.ClientId;
                sessionItems = clientConnection.Session.Items;
            }

            var interceptorContext = new MqttApplicationMessageInterceptorContext
            {
                ClientId = senderClientId,
                SessionItems = sessionItems,
                AcceptPublish = true,
                ApplicationMessage = applicationMessage,
                CloseConnection = false
            };

            await interceptor.InterceptApplicationMessagePublishAsync(interceptorContext).ConfigureAwait(false);
            return interceptorContext;
        }

        MqttClientSession GetClientSession(string clientId)
        {
            lock (_clientSessions)
            {
                if (!_clientSessions.TryGetValue(clientId, out var session))
                {
                    throw new InvalidOperationException($"Client session '{clientId}' is unknown.");
                }

                return session;
            }
        }

        MqttClientConnection CreateConnection(MqttConnectPacket connectPacket, IMqttChannelAdapter channelAdapter, MqttClientSession session)
        {
            return new MqttClientConnection(
                connectPacket,
                channelAdapter,
                session,
                _options,
                this,
                _rootLogger);
        }

        MqttClientSession CreateSession(string clientId, IDictionary<object, object> sessionItems)
        {
            return new MqttClientSession(
                clientId,
                sessionItems,
                _eventDispatcher,
                _options,
                _retainedMessagesManager);
        }
    }
}