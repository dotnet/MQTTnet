using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public sealed class MqttClientSessionsManager : IDisposable
    {
        readonly MqttPacketFactories _packetFactories = new MqttPacketFactories();
        
        readonly AsyncLock _createConnectionSyncRoot = new AsyncLock();
        readonly Dictionary<string, MqttClient> _clients = new Dictionary<string, MqttClient>(4096);
        readonly Dictionary<string, MqttSession> _sessions = new Dictionary<string, MqttSession>(4096);

        readonly IDictionary<object, object> _serverSessionItems = new ConcurrentDictionary<object, object>();

        readonly MqttConnAckPacketFactory _connAckPacketFactory = new MqttConnAckPacketFactory();
        
        readonly MqttRetainedMessagesManager _retainedMessagesManager;
        readonly MqttServerEventContainer _eventContainer;
        readonly MqttServerOptions _options;
        readonly MqttNetSourceLogger _logger;
        readonly IMqttNetLogger _rootLogger;

        public MqttClientSessionsManager(
            MqttServerOptions options,
            MqttRetainedMessagesManager retainedMessagesManager,
            MqttServerEventContainer eventContainer,
            IMqttNetLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            _logger = logger.WithSource(nameof(MqttClientSessionsManager));
            _rootLogger = logger;
            
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _retainedMessagesManager = retainedMessagesManager ?? throw new ArgumentNullException(nameof(retainedMessagesManager));
            _eventContainer = eventContainer ?? throw new ArgumentNullException(nameof(eventContainer));
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
            MqttClient client = null;

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
                    // Send failure response here without preparing a connection and session!
                    connAckPacket = _connAckPacketFactory.Create(connectionValidatorContext);
                    await channelAdapter.SendPacketAsync(connAckPacket, cancellationToken).ConfigureAwait(false);
                    return;
                }

                connAckPacket = _connAckPacketFactory.Create(connectionValidatorContext);
  
                // Pass connAckPacket so that IsSessionPresent flag can be set if the client session already exists
                client = await CreateClientConnection(connectPacket, connAckPacket, channelAdapter, connectionValidatorContext.SessionItems).ConfigureAwait(false);

                await client.SendPacketAsync(connAckPacket, cancellationToken).ConfigureAwait(false);

                await _eventContainer.ClientConnectedEvent.InvokeAsync(() => new ClientConnectedEventArgs
                {
                    ClientId = connectPacket.ClientId,
                    UserName = connectPacket.Username,
                    ProtocolVersion = channelAdapter.PacketFormatterAdapter.ProtocolVersion,
                    Endpoint = channelAdapter.Endpoint
                }).ConfigureAwait(false);
                
                await client.RunAsync().ConfigureAwait(false);
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
                if (client != null)
                {
                    if (client.Id != null)
                    {
                        // in case it is a takeover _clientConnections already contains the new connection
                        if (!client.IsTakenOver)
                        {
                            lock (_clients)
                            {
                                _clients.Remove(client.Id);
                            }

                            if (!_options.EnablePersistentSessions)
                            {
                                await DeleteSessionAsync(client.Id).ConfigureAwait(false);
                            }
                        }
                    }

                    var endpoint = client.Endpoint;

                    if (client.Id != null && !client.IsTakenOver)
                    {
                        await _eventContainer.ClientDisconnectedEvent.InvokeAsync(() => new ClientDisconnectedEventArgs
                        {
                            ClientId = client.Id,
                            DisconnectType = client.IsCleanDisconnect ? MqttClientDisconnectType.Clean : MqttClientDisconnectType.NotClean,
                            Endpoint = endpoint
                        }).ConfigureAwait(false);
                    }
                }

                using (var timeout = new CancellationTokenSource(_options.DefaultCommunicationTimeout))
                {
                    await channelAdapter.DisconnectAsync(timeout.Token).ConfigureAwait(false);    
                }
            }
        }

        public async Task CloseAllConnectionsAsync()
        {
            List<MqttClient> connections;
            lock (_clients)
            {
                connections = _clients.Values.ToList();
                _clients.Clear();
            }

            foreach (var connection in connections)
            {
                await connection.StopAsync(MqttClientDisconnectReason.NormalDisconnection).ConfigureAwait(false);
            }
        }

        public List<MqttClient> GetConnections()
        {
            lock (_clients)
            {
                return _clients.Values.ToList();
            }
        }

        public Task<IList<IMqttClientStatus>> GetClientStatusAsync()
        {
            var result = new List<IMqttClientStatus>();

            lock (_clients)
            {
                foreach (var connection in _clients.Values)
                {
                    var clientStatus = new MqttClientStatus(connection)
                    {
                        Session = new MqttSessionStatus(connection.Session)
                    };

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
                    var sessionStatus = new MqttSessionStatus(session);
                    result.Add(sessionStatus);
                }
            }

            return Task.FromResult((IList<IMqttSessionStatus>)result);
        }

        // public void DispatchPublishPacket(MqttPublishPacket publishPacket, MqttClientConnection sender)
        // {
        //     if (publishPacket == null) throw new ArgumentNullException(nameof(publishPacket));
        //
        //     _messageQueue.Add(new MqttPendingApplicationMessage(publishPacket, sender));
        // }
        
        public async Task SubscribeAsync(string clientId, ICollection<MqttTopicFilter> topicFilters)
        {
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            var fakeSubscribePacket = new MqttSubscribePacket();
            fakeSubscribePacket.TopicFilters.AddRange(topicFilters);

            var clientSession = GetClientSession(clientId);

            var subscribeResult = await clientSession.SubscriptionsManager.Subscribe(fakeSubscribePacket, CancellationToken.None).ConfigureAwait(false);
            
            foreach (var retainedApplicationMessage in subscribeResult.RetainedApplicationMessages)
            {
                var publishPacket = _packetFactories.Publish.Create(retainedApplicationMessage.ApplicationMessage);
                clientSession.EnqueuePacket(new MqttPacketBusItem(publishPacket));
            }
        }

        public Task UnsubscribeAsync(string clientId, ICollection<string> topicFilters)
        {
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            var fakeUnsubscribePacket = new MqttUnsubscribePacket();
            fakeUnsubscribePacket.TopicFilters.AddRange(topicFilters);
            
            return GetClientSession(clientId).SubscriptionsManager.Unsubscribe(fakeUnsubscribePacket, CancellationToken.None);
        }

        public async Task DeleteSessionAsync(string clientId)
        {
            MqttClient connection;
            MqttSession session;
            
            lock (_clients)
            {
                _clients.TryGetValue(clientId, out connection);
            }

            lock (_sessions)
            {
                _sessions.TryGetValue(clientId, out session);
                _sessions.Remove(clientId);
            }

            try
            {
                if (connection != null)
                {
                    await connection.StopAsync(MqttClientDisconnectReason.NormalDisconnection).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"Error while deleting session '{clientId}'.");
            }

            try
            {
                session?.OnDeleted();
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"Error while executing session deleted event for session '{clientId}'.");
            }
            
            session?.Dispose();
            
            _logger.Verbose("Session for client '{0}' deleted.", clientId);
        }

        // public void Dispose()
        // {
        //     _messageQueue?.Dispose();
        // }

        // async Task TryProcessQueuedApplicationMessagesAsync(CancellationToken cancellationToken)
        // {
        //     // Make sure all queued messages are processed before server stops.
        //     while (!cancellationToken.IsCancellationRequested || _messageQueue.Any())
        //     {
        //         try
        //         {
        //             await TryProcessNextQueuedApplicationMessage().ConfigureAwait(false);
        //         }
        //         catch (OperationCanceledException)
        //         {
        //         }
        //         catch (Exception exception)
        //         {
        //             _logger.Error(exception, "Unhandled exception while processing queued application messages.");
        //         }
        //     }
        // }

        public async Task DispatchPublishPacket(string senderClientId, MqttApplicationMessage applicationMessage)
        {
            try
            {
                if (applicationMessage.Retain)
                {
                    await _retainedMessagesManager.UpdateMessage(senderClientId, applicationMessage).ConfigureAwait(false);
                }

                var deliveryCount = 0;
                List<MqttSession> sessions;
                lock (_sessions)
                {
                    sessions = _sessions.Values.ToList();
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

                    var newPublishPacket = _packetFactories.Publish.Create(applicationMessage);
                    newPublishPacket.QualityOfServiceLevel = checkSubscriptionsResult.QualityOfServiceLevel;
                    newPublishPacket.Properties.SubscriptionIdentifiers.AddRange(checkSubscriptionsResult.SubscriptionIdentifiers);

                    if (newPublishPacket.QualityOfServiceLevel > 0)
                    {
                        newPublishPacket.PacketIdentifier = clientSession.PacketIdentifierProvider.GetNextPacketIdentifier();
                    }
                    
                    if (checkSubscriptionsResult.RetainAsPublished)
                    {
                        // Transfer the original retain state from the publisher.
                        // This is a MQTTv5 feature.
                        newPublishPacket.Retain = applicationMessage.Retain;
                    }
                    else
                    {
                        newPublishPacket.Retain = false;
                    }
                    
                    clientSession.EnqueuePacket(new MqttPacketBusItem(newPublishPacket));
                    deliveryCount++;
                    
                    _logger.Verbose("Client '{0}': Queued PUBLISH packet with topic '{1}'.", clientSession.Id, applicationMessage.Topic);
                }

                if (deliveryCount == 0)
                {
                    await _eventContainer.ApplicationMessageNotConsumedEvent.InvokeAsync(() => new ApplicationMessageNotConsumedEventArgs
                    {
                        ApplicationMessage = applicationMessage,
                        SenderClientId = senderClientId
                    }).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Unhandled exception while processing next queued application message.");
            }
        }

        async Task<ValidatingConnectionEventArgs> ValidateConnection(MqttConnectPacket connectPacket, IMqttChannelAdapter channelAdapter)
        {
            var context = new ValidatingConnectionEventArgs(connectPacket, channelAdapter)
            {
                SessionItems = new ConcurrentDictionary<object, object>()
            };
            
            await _eventContainer.ValidatingConnectionEvent.InvokeAsync(context).ConfigureAwait(false);

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

        async Task<MqttClient> CreateClientConnection(
            MqttConnectPacket connectPacket,
            MqttConnAckPacket connAckPacket,
            IMqttChannelAdapter channelAdapter, 
            IDictionary<object, object> sessionItems)
        {
            MqttClient connection;

            using (await _createConnectionSyncRoot.WaitAsync(CancellationToken.None).ConfigureAwait(false))
            {
                MqttSession session;
                lock (_sessions)
                {
                    if (!_sessions.TryGetValue(connectPacket.ClientId, out session))
                    {
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
                            connAckPacket.IsSessionPresent = true;
                            session.Recover();
                        }
                    }

                    _sessions[connectPacket.ClientId] = session;
                }

                if (!connAckPacket.IsSessionPresent)
                {
                    var preparingSessionEventArgs = new PreparingSessionEventArgs();
                    await _eventContainer.PreparingSessionEvent.InvokeAsync(preparingSessionEventArgs).ConfigureAwait(false);
                    
                    // TODO: Import subscriptions etc.
                    //session.SubscriptionsManager.Subscribe()
                }

                MqttClient existing;

                lock (_clients)
                {
                    _clients.TryGetValue(connectPacket.ClientId, out existing);
                    connection = CreateConnection(connectPacket, channelAdapter, session);

                    _clients[connectPacket.ClientId] = connection;
                }

                if (existing != null)
                {
                    existing.IsTakenOver = true;
                    await existing.StopAsync(MqttClientDisconnectReason.SessionTakenOver).ConfigureAwait(false);
                    
                    await _eventContainer.ClientDisconnectedEvent.InvokeAsync(() => new ClientDisconnectedEventArgs
                    {
                        ClientId = existing.Id,
                        DisconnectType = MqttClientDisconnectType.Takeover,
                        Endpoint = existing.Endpoint
                    }).ConfigureAwait(false);
                }
            }

            return connection;
        }

        // async Task<InterceptingMqttClientPublishEventArgs> InterceptApplicationMessageAsync(
        //     IMqttServerApplicationMessageInterceptor interceptor,
        //     MqttClientConnection clientConnection,
        //     MqttApplicationMessage applicationMessage)
        // {
        //     string senderClientId;
        //     IDictionary<object, object> sessionItems;
        //
        //     var messageIsFromServer = clientConnection == null;
        //     if (messageIsFromServer)
        //     {
        //         senderClientId = _options.ClientId;
        //         sessionItems = _serverSessionItems;
        //     }
        //     else
        //     {
        //         senderClientId = clientConnection.Id;
        //         sessionItems = clientConnection.Session.Items;
        //     }
        //
        //     var interceptorContext = new InterceptingMqttClientPublishEventArgs
        //     {
        //         ClientId = senderClientId,
        //         SessionItems = sessionItems,
        //         ProcessPublish = true,
        //         ApplicationMessage = applicationMessage,
        //         CloseConnection = false
        //     };
        //
        //     await interceptor.InterceptApplicationMessagePublishAsync(interceptorContext).ConfigureAwait(false);
        //     return interceptorContext;
        // }

        MqttSession GetClientSession(string clientId)
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

        MqttClient CreateConnection(MqttConnectPacket connectPacket, IMqttChannelAdapter channelAdapter, MqttSession session)
        {
            return new MqttClient(
                connectPacket,
                channelAdapter,
                session,
                _options,
                _eventContainer,
                this,
                _rootLogger);
        }

        MqttSession CreateSession(string clientId, IDictionary<object, object> sessionItems)
        {
            _logger.Verbose("Created a new session for client '{0}'.", clientId);
            
            return new MqttSession(
                clientId,
                sessionItems,
                _options,
                _eventContainer,
                _retainedMessagesManager,
                this);
        }

        public void Dispose()
        {
            _createConnectionSyncRoot?.Dispose();

            foreach (var clientConnection in _clients.Values)
            {
                clientConnection.Dispose();
            }

            foreach (var session in _sessions.Values)
            {
                session.Dispose();
            }
        }
    }
}