// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public sealed class MqttClientSessionsManager : ISubscriptionChangedNotification, IDisposable
    {
        readonly Dictionary<string, MqttClient> _clients = new Dictionary<string, MqttClient>(4096);

        readonly AsyncLock _createConnectionSyncRoot = new AsyncLock();

        readonly MqttServerEventContainer _eventContainer;
        readonly MqttNetSourceLogger _logger;
        readonly MqttServerOptions _options;
        readonly MqttPacketFactories _packetFactories = new MqttPacketFactories();

        readonly MqttRetainedMessagesManager _retainedMessagesManager;
        readonly IMqttNetLogger _rootLogger;

        // The _sessions dictionary contains all session, the _subscriberSessions hash set contains subscriber sessions only.
        // See the MqttSubscription object for a detailed explanation.
        readonly Dictionary<string, MqttSession> _sessions = new Dictionary<string, MqttSession>(4096);

        readonly object _sessionsManagementLock = new object();
        readonly HashSet<MqttSession> _subscriberSessions = new HashSet<MqttSession>();

        public MqttClientSessionsManager(
            MqttServerOptions options,
            MqttRetainedMessagesManager retainedMessagesManager,
            MqttServerEventContainer eventContainer,
            IMqttNetLogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _logger = logger.WithSource(nameof(MqttClientSessionsManager));
            _rootLogger = logger;

            _options = options ?? throw new ArgumentNullException(nameof(options));
            _retainedMessagesManager = retainedMessagesManager ?? throw new ArgumentNullException(nameof(retainedMessagesManager));
            _eventContainer = eventContainer ?? throw new ArgumentNullException(nameof(eventContainer));
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
                await connection.StopAsync(MqttDisconnectReasonCode.NormalDisconnection).ConfigureAwait(false);
            }
        }

        public async Task DeleteSessionAsync(string clientId)
        {
            _logger.Verbose("Deleting session for client '{0}'.", clientId);

            MqttClient connection;

            lock (_clients)
            {
                _clients.TryGetValue(clientId, out connection);
            }

            MqttSession session;

            lock (_sessionsManagementLock)
            {
                _sessions.TryGetValue(clientId, out session);
                _sessions.Remove(clientId);

                if (session != null)
                {
                    _subscriberSessions.Remove(session);
                }
            }

            try
            {
                if (connection != null)
                {
                    await connection.StopAsync(MqttDisconnectReasonCode.NormalDisconnection).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"Error while deleting session '{clientId}'.");
            }

            try
            {
                if (_eventContainer.SessionDeletedEvent.HasHandlers && session != null)
                {
                    var eventArgs = new SessionDeletedEventArgs(clientId, session.Items);
                    await _eventContainer.SessionDeletedEvent.TryInvokeAsync(eventArgs, _logger).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"Error while executing session deleted event for session '{clientId}'.");
            }

            session?.Dispose();

            _logger.Verbose("Session for client '{0}' deleted.", clientId);
        }

        public async Task DispatchApplicationMessage(string senderId, MqttApplicationMessage applicationMessage)
        {
            try
            {
                if (applicationMessage.Retain)
                {
                    await _retainedMessagesManager.UpdateMessage(senderId, applicationMessage).ConfigureAwait(false);
                }

                var deliveryCount = 0;
                List<MqttSession> subscriberSessions;
                lock (_sessionsManagementLock)
                {
                    // only subscriber clients are of interest here.
                    subscriberSessions = _subscriberSessions.ToList();
                }

                // Calculate application message topic hash once for subscription checks
                MqttSubscription.CalculateTopicHash(applicationMessage.Topic, out var topicHash, out _, out _);

                foreach (var session in subscriberSessions)
                {
                    var checkSubscriptionsResult = session.SubscriptionsManager.CheckSubscriptions(
                        applicationMessage.Topic,
                        topicHash,
                        applicationMessage.QualityOfServiceLevel,
                        senderId);

                    if (!checkSubscriptionsResult.IsSubscribed)
                    {
                        continue;
                    }

                    var newPublishPacket = _packetFactories.Publish.Create(applicationMessage);
                    newPublishPacket.QualityOfServiceLevel = checkSubscriptionsResult.QualityOfServiceLevel;
                    newPublishPacket.SubscriptionIdentifiers = checkSubscriptionsResult.SubscriptionIdentifiers;

                    if (newPublishPacket.QualityOfServiceLevel > 0)
                    {
                        newPublishPacket.PacketIdentifier = session.PacketIdentifierProvider.GetNextPacketIdentifier();
                    }

                    if (checkSubscriptionsResult.RetainAsPublished)
                    {
                        // Transfer the original retain state from the publisher. This is a MQTTv5 feature.
                        newPublishPacket.Retain = applicationMessage.Retain;
                    }
                    else
                    {
                        newPublishPacket.Retain = false;
                    }

                    session.EnqueueDataPacket(new MqttPacketBusItem(newPublishPacket));
                    deliveryCount++;

                    _logger.Verbose("Client '{0}': Queued PUBLISH packet with topic '{1}'.", session.Id, applicationMessage.Topic);
                }

                await FireApplicationMessageNotConsumedEvent(applicationMessage, deliveryCount, senderId);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Unhandled exception while processing next queued application message.");
            }
        }

        public void Dispose()
        {
            _createConnectionSyncRoot.Dispose();

            lock (_sessionsManagementLock)
            {
                foreach (var sessionItem in _sessions)
                {
                    sessionItem.Value.Dispose();
                }
            }
        }

        public MqttClient GetClient(string id)
        {
            lock (_clients)
            {
                if (!_clients.TryGetValue(id, out var client))
                {
                    throw new InvalidOperationException($"Client with ID '{id}' not found.");
                }

                return client;
            }
        }

        public List<MqttClient> GetClients()
        {
            lock (_clients)
            {
                return _clients.Values.ToList();
            }
        }

        public Task<IList<MqttClientStatus>> GetClientStatusesAsync()
        {
            var result = new List<MqttClientStatus>();

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

            return Task.FromResult((IList<MqttClientStatus>)result);
        }

        public Task<IList<MqttSessionStatus>> GetSessionStatusAsync()
        {
            var result = new List<MqttSessionStatus>();

            lock (_sessionsManagementLock)
            {
                foreach (var sessionItem in _sessions)
                {
                    var sessionStatus = new MqttSessionStatus(sessionItem.Value);
                    result.Add(sessionStatus);
                }
            }

            return Task.FromResult((IList<MqttSessionStatus>)result);
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

                var validatingConnectionEventArgs = await ValidateConnection(connectPacket, channelAdapter).ConfigureAwait(false);
                var connAckPacket = _packetFactories.ConnAck.Create(validatingConnectionEventArgs);

                if (validatingConnectionEventArgs.ReasonCode != MqttConnectReasonCode.Success)
                {
                    // Send failure response here without preparing a connection and session!
                    await channelAdapter.SendPacketAsync(connAckPacket, cancellationToken).ConfigureAwait(false);
                    return;
                }

                // Pass connAckPacket so that IsSessionPresent flag can be set if the client session already exists.
                client = await CreateClientConnection(connectPacket, connAckPacket, channelAdapter, validatingConnectionEventArgs).ConfigureAwait(false);

                await client.SendPacketAsync(connAckPacket, cancellationToken).ConfigureAwait(false);

                if (_eventContainer.ClientConnectedEvent.HasHandlers)
                {
                    var eventArgs = new ClientConnectedEventArgs(
                        connectPacket.ClientId,
                        connectPacket.Username,
                        channelAdapter.PacketFormatterAdapter.ProtocolVersion,
                        channelAdapter.Endpoint,
                        client.Session.Items);

                    await _eventContainer.ClientConnectedEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
                }

                await client.RunAsync().ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
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

                            if (!_options.EnablePersistentSessions || !client.Session.IsPersistent)
                            {
                                await DeleteSessionAsync(client.Id).ConfigureAwait(false);
                            }
                        }
                    }

                    var endpoint = client.Endpoint;

                    if (client.Id != null && !client.IsTakenOver && _eventContainer.ClientDisconnectedEvent.HasHandlers)
                    {
                        var disconnectType = client.DisconnectPacket != null ? MqttClientDisconnectType.Clean : MqttClientDisconnectType.NotClean;
                        var eventArgs = new ClientDisconnectedEventArgs(client.Id, client.DisconnectPacket, disconnectType, endpoint, client.Session.Items);

                        await _eventContainer.ClientDisconnectedEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
                    }
                }

                using (var timeout = new CancellationTokenSource(_options.DefaultCommunicationTimeout))
                {
                    await channelAdapter.DisconnectAsync(timeout.Token).ConfigureAwait(false);
                }
            }
        }

        public void OnSubscriptionsAdded(MqttSession clientSession, List<string> topics)
        {
            lock (_sessionsManagementLock)
            {
                if (!clientSession.HasSubscribedTopics)
                {
                    // first subscribed topic
                    _subscriberSessions.Add(clientSession);
                }

                foreach (var topic in topics)
                {
                    clientSession.AddSubscribedTopic(topic);
                }
            }
        }

        public void OnSubscriptionsRemoved(MqttSession clientSession, List<string> subscriptionTopics)
        {
            lock (_sessionsManagementLock)
            {
                foreach (var subscriptionTopic in subscriptionTopics)
                {
                    clientSession.RemoveSubscribedTopic(subscriptionTopic);
                }

                if (!clientSession.HasSubscribedTopics)
                {
                    // last subscription removed
                    _subscriberSessions.Remove(clientSession);
                }
            }
        }

        public void Start()
        {
            if (!_options.EnablePersistentSessions)
            {
                _sessions.Clear();
            }
        }

        public async Task SubscribeAsync(string clientId, ICollection<MqttTopicFilter> topicFilters)
        {
            if (clientId == null)
            {
                throw new ArgumentNullException(nameof(clientId));
            }

            if (topicFilters == null)
            {
                throw new ArgumentNullException(nameof(topicFilters));
            }

            var fakeSubscribePacket = new MqttSubscribePacket();
            fakeSubscribePacket.TopicFilters.AddRange(topicFilters);

            var clientSession = GetClientSession(clientId);

            var subscribeResult = await clientSession.SubscriptionsManager.Subscribe(fakeSubscribePacket, CancellationToken.None).ConfigureAwait(false);

            if (subscribeResult.RetainedMessages != null)
            {
                foreach (var retainedApplicationMessage in subscribeResult.RetainedMessages)
                {
                    var publishPacket = _packetFactories.Publish.Create(retainedApplicationMessage.ApplicationMessage);
                    clientSession.EnqueueDataPacket(new MqttPacketBusItem(publishPacket));
                }
            }
        }

        public Task UnsubscribeAsync(string clientId, ICollection<string> topicFilters)
        {
            if (clientId == null)
            {
                throw new ArgumentNullException(nameof(clientId));
            }

            if (topicFilters == null)
            {
                throw new ArgumentNullException(nameof(topicFilters));
            }

            var fakeUnsubscribePacket = new MqttUnsubscribePacket();
            fakeUnsubscribePacket.TopicFilters.AddRange(topicFilters);

            return GetClientSession(clientId).SubscriptionsManager.Unsubscribe(fakeUnsubscribePacket, CancellationToken.None);
        }

        MqttClient CreateClient(MqttConnectPacket connectPacket, IMqttChannelAdapter channelAdapter, MqttSession session)
        {
            return new MqttClient(connectPacket, channelAdapter, session, _options, _eventContainer, this, _rootLogger);
        }

        async Task<MqttClient> CreateClientConnection(
            MqttConnectPacket connectPacket,
            MqttConnAckPacket connAckPacket,
            IMqttChannelAdapter channelAdapter,
            ValidatingConnectionEventArgs validatingConnectionEventArgs)
        {
            MqttClient client;

            bool sessionShouldPersist;

            if (validatingConnectionEventArgs.ProtocolVersion == MqttProtocolVersion.V500)
            {
                // MQTT 5.0 section 3.1.2.11.2
                // The Client and Server MUST store the Session State after the Network Connection is closed if the Session Expiry Interval is greater than 0 [MQTT-3.1.2-23].
                //
                // A Client that only wants to process messages while connected will set the Clean Start to 1 and set the Session Expiry Interval to 0.
                // It will not receive Application Messages published before it connected and has to subscribe afresh to any topics that it is interested
                // in each time it connects.

                // Persist if SessionExpiryInterval != 0, but may start with a clean session
                sessionShouldPersist = validatingConnectionEventArgs.SessionExpiryInterval != 0;
            }
            else
            {
                // MQTT 3.1.1 section 3.1.2.4: persist only if 'not CleanSession'
                //
                // If CleanSession is set to 1, the Client and Server MUST discard any previous Session and start a new one.
                // This Session lasts as long as the Network Connection. State data associated with this Session MUST NOT be
                // reused in any subsequent Session [MQTT-3.1.2-6].

                sessionShouldPersist = !connectPacket.CleanSession;
            }

            using (await _createConnectionSyncRoot.EnterAsync().ConfigureAwait(false))
            {
                MqttSession session;
                lock (_sessionsManagementLock)
                {
                    if (!_sessions.TryGetValue(connectPacket.ClientId, out session))
                    {
                        session = CreateSession(connectPacket.ClientId, validatingConnectionEventArgs.SessionItems, sessionShouldPersist);
                    }
                    else
                    {
                        if (connectPacket.CleanSession)
                        {
                            _logger.Verbose("Deleting existing session of client '{0}' due to clean start.", connectPacket.ClientId);
                            session = CreateSession(connectPacket.ClientId, validatingConnectionEventArgs.SessionItems, sessionShouldPersist);
                        }
                        else
                        {
                            _logger.Verbose("Reusing existing session of client '{0}'.", connectPacket.ClientId);
                            // Session persistence could change for MQTT 5 clients that reconnect with different SessionExpiryInterval
                            session.IsPersistent = sessionShouldPersist;
                            connAckPacket.IsSessionPresent = true;
                            session.Recover();
                        }
                    }

                    _sessions[connectPacket.ClientId] = session;
                }

                if (!connAckPacket.IsSessionPresent)
                {
                    // TODO: This event is not yet final. It can already be used but restoring sessions from storage will be added later!
                    var preparingSessionEventArgs = new PreparingSessionEventArgs();
                    await _eventContainer.PreparingSessionEvent.InvokeAsync(preparingSessionEventArgs).ConfigureAwait(false);
                }

                MqttClient existingClient;

                lock (_clients)
                {
                    _clients.TryGetValue(connectPacket.ClientId, out existingClient);
                    client = CreateClient(connectPacket, channelAdapter, session);

                    _clients[connectPacket.ClientId] = client;
                }

                if (existingClient != null)
                {
                    existingClient.IsTakenOver = true;
                    await existingClient.StopAsync(MqttDisconnectReasonCode.SessionTakenOver).ConfigureAwait(false);

                    if (_eventContainer.ClientConnectedEvent.HasHandlers)
                    {
                        var eventArgs = new ClientDisconnectedEventArgs(
                            existingClient.Id,
                            null,
                            MqttClientDisconnectType.Takeover,
                            existingClient.Endpoint,
                            existingClient.Session.Items);
                        
                        await _eventContainer.ClientDisconnectedEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
                    }
                }
            }

            return client;
        }

        MqttSession CreateSession(string clientId, IDictionary sessionItems, bool isPersistent)
        {
            _logger.Verbose("Created new session for client '{0}'.", clientId);

            return new MqttSession(clientId, isPersistent, sessionItems, _options, _eventContainer, _retainedMessagesManager, this);
        }

        async Task FireApplicationMessageNotConsumedEvent(MqttApplicationMessage applicationMessage, int deliveryCount, string senderId)
        {
            if (deliveryCount > 0)
            {
                return;
            }

            if (!_eventContainer.ApplicationMessageNotConsumedEvent.HasHandlers)
            {
                return;
            }

            var eventArgs = new ApplicationMessageNotConsumedEventArgs(applicationMessage, senderId);
            await _eventContainer.ApplicationMessageNotConsumedEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
        }

        MqttSession GetClientSession(string clientId)
        {
            lock (_sessionsManagementLock)
            {
                if (!_sessions.TryGetValue(clientId, out var session))
                {
                    throw new InvalidOperationException($"Client session '{clientId}' is unknown.");
                }

                return session;
            }
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

        async Task<ValidatingConnectionEventArgs> ValidateConnection(MqttConnectPacket connectPacket, IMqttChannelAdapter channelAdapter)
        {
            var eventArgs = new ValidatingConnectionEventArgs(connectPacket, channelAdapter)
            {
                SessionItems = new ConcurrentDictionary<object, object>()
            };

            await _eventContainer.ValidatingConnectionEvent.InvokeAsync(eventArgs).ConfigureAwait(false);

            // Check the client ID and set a random one if supported.
            if (string.IsNullOrEmpty(connectPacket.ClientId) && channelAdapter.PacketFormatterAdapter.ProtocolVersion == MqttProtocolVersion.V500)
            {
                connectPacket.ClientId = eventArgs.AssignedClientIdentifier;
            }

            if (string.IsNullOrEmpty(connectPacket.ClientId))
            {
                eventArgs.ReasonCode = MqttConnectReasonCode.ClientIdentifierNotValid;
            }

            return eventArgs;
        }
    }
}