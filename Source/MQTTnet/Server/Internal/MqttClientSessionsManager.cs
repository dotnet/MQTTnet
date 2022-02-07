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
    public sealed class MqttClientSessionsManager : IDisposable
    {
        readonly Dictionary<string, MqttClient> _clients = new Dictionary<string, MqttClient>(4096);

        readonly AsyncLock _createConnectionSyncRoot = new AsyncLock();
        readonly MqttServerEventContainer _eventContainer;
        readonly MqttNetSourceLogger _logger;
        readonly MqttServerOptions _options;
        readonly MqttPacketFactories _packetFactories = new MqttPacketFactories();

        readonly MqttRetainedMessagesManager _retainedMessagesManager;
        readonly IMqttNetLogger _rootLogger;
        readonly Dictionary<string, MqttSession> _sessions = new Dictionary<string, MqttSession>(4096);

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
                await connection.StopAsync(MqttDisconnectReasonCode.NormalDisconnection)
                    .ConfigureAwait(false);
            }
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
                    await connection.StopAsync(MqttDisconnectReasonCode.NormalDisconnection)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"Error while deleting session '{clientId}'.");
            }

            try
            {
                await _eventContainer.SessionDeletedEvent.TryInvokeAsync(
                        new SessionDeletedEventArgs
                        {
                            Id = session.Id
                        },
                        _logger)
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"Error while executing session deleted event for session '{clientId}'.");
            }

            session?.Dispose();

            _logger.Verbose("Session for client '{0}' deleted.", clientId);
        }

        public async Task DispatchPublishPacket(string senderClientId, MqttApplicationMessage applicationMessage)
        {
            try
            {
                if (applicationMessage.Retain)
                {
                    await _retainedMessagesManager.UpdateMessage(senderClientId, applicationMessage)
                        .ConfigureAwait(false);
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
                    newPublishPacket.SubscriptionIdentifiers = checkSubscriptionsResult.SubscriptionIdentifiers;

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
                    await _eventContainer.ApplicationMessageNotConsumedEvent.InvokeAsync(
                            new ApplicationMessageNotConsumedEventArgs
                            {
                                ApplicationMessage = applicationMessage,
                                SenderClientId = senderClientId
                            })
                        .ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Unhandled exception while processing next queued application message.");
            }
        }

        public void Dispose()
        {
            _createConnectionSyncRoot?.Dispose();

            foreach (var session in _sessions.Values)
            {
                session.Dispose();
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

        public Task<IList<MqttClientStatus>> GetClientStatusAsync()
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

            lock (_sessions)
            {
                foreach (var session in _sessions.Values)
                {
                    var sessionStatus = new MqttSessionStatus(session);
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
                var connectPacket = await ReceiveConnectPacket(channelAdapter, cancellationToken)
                    .ConfigureAwait(false);
                if (connectPacket == null)
                {
                    // Nothing was received in time etc.
                    return;
                }

                var validatingConnectionEventArgs = await ValidateConnection(connectPacket, channelAdapter)
                    .ConfigureAwait(false);
                var connAckPacket = _packetFactories.ConnAck.Create(validatingConnectionEventArgs);

                if (validatingConnectionEventArgs.ReasonCode != MqttConnectReasonCode.Success)
                {
                    // Send failure response here without preparing a connection and session!
                    await channelAdapter.SendPacketAsync(connAckPacket, cancellationToken)
                        .ConfigureAwait(false);
                    return;
                }

                // Pass connAckPacket so that IsSessionPresent flag can be set if the client session already exists.
                client = await CreateClientConnection(connectPacket, connAckPacket, channelAdapter, validatingConnectionEventArgs)
                    .ConfigureAwait(false);

                await client.SendPacketAsync(connAckPacket, cancellationToken)
                    .ConfigureAwait(false);

                await _eventContainer.ClientConnectedEvent.InvokeAsync(
                        () => new ClientConnectedEventArgs
                        {
                            ClientId = connectPacket.ClientId,
                            UserName = connectPacket.Username,
                            ProtocolVersion = channelAdapter.PacketFormatterAdapter.ProtocolVersion,
                            Endpoint = channelAdapter.Endpoint
                        })
                    .ConfigureAwait(false);

                await client.RunAsync()
                    .ConfigureAwait(false);
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
                                await DeleteSessionAsync(client.Id)
                                    .ConfigureAwait(false);
                            }
                        }
                    }

                    var endpoint = client.Endpoint;

                    if (client.Id != null && !client.IsTakenOver)
                    {
                        await _eventContainer.ClientDisconnectedEvent.InvokeAsync(
                                () => new ClientDisconnectedEventArgs
                                {
                                    ClientId = client.Id,
                                    DisconnectType = client.IsCleanDisconnect ? MqttClientDisconnectType.Clean : MqttClientDisconnectType.NotClean,
                                    Endpoint = endpoint
                                })
                            .ConfigureAwait(false);
                    }
                }

                using (var timeout = new CancellationTokenSource(_options.DefaultCommunicationTimeout))
                {
                    await channelAdapter.DisconnectAsync(timeout.Token)
                        .ConfigureAwait(false);
                }
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

            var subscribeResult = await clientSession.SubscriptionsManager.Subscribe(fakeSubscribePacket, CancellationToken.None)
                .ConfigureAwait(false);

            foreach (var retainedApplicationMessage in subscribeResult.RetainedApplicationMessages)
            {
                var publishPacket = _packetFactories.Publish.Create(retainedApplicationMessage.ApplicationMessage);
                clientSession.EnqueuePacket(new MqttPacketBusItem(publishPacket));
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

            return GetClientSession(clientId)
                .SubscriptionsManager.Unsubscribe(fakeUnsubscribePacket, CancellationToken.None);
        }

        async Task<MqttClient> CreateClientConnection(
            MqttConnectPacket connectPacket,
            MqttConnAckPacket connAckPacket,
            IMqttChannelAdapter channelAdapter,
            ValidatingConnectionEventArgs validatingConnectionEventArgs)
        {
            MqttClient connection;

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

            using (await _createConnectionSyncRoot.WaitAsync(CancellationToken.None)
                       .ConfigureAwait(false))
            {
                MqttSession session;
                lock (_sessions)
                {
                    if (!_sessions.TryGetValue(connectPacket.ClientId, out session))
                    {
                        session = CreateSession(connectPacket.ClientId, validatingConnectionEventArgs.SessionItems, sessionShouldPersist);
                    }
                    else
                    {
                        if (connectPacket.CleanSession)
                        {
                            _logger.Verbose("Deleting existing session of client '{0}'.", connectPacket.ClientId);
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
                    var preparingSessionEventArgs = new PreparingSessionEventArgs();
                    await _eventContainer.PreparingSessionEvent.InvokeAsync(preparingSessionEventArgs)
                        .ConfigureAwait(false);

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
                    await existing.StopAsync(MqttDisconnectReasonCode.SessionTakenOver)
                        .ConfigureAwait(false);

                    await _eventContainer.ClientDisconnectedEvent.InvokeAsync(
                            () => new ClientDisconnectedEventArgs
                            {
                                ClientId = existing.Id,
                                DisconnectType = MqttClientDisconnectType.Takeover,
                                Endpoint = existing.Endpoint
                            })
                        .ConfigureAwait(false);
                }
            }

            return connection;
        }

        MqttClient CreateConnection(MqttConnectPacket connectPacket, IMqttChannelAdapter channelAdapter, MqttSession session)
        {
            return new MqttClient(connectPacket, channelAdapter, session, _options, _eventContainer, this, _rootLogger);
        }

        MqttSession CreateSession(string clientId, IDictionary sessionItems, bool isPersistent)
        {
            _logger.Verbose("Created a new session for client '{0}'.", clientId);

            return new MqttSession(clientId, isPersistent, sessionItems, _options, _eventContainer, _retainedMessagesManager, this);
        }

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

        async Task<MqttConnectPacket> ReceiveConnectPacket(IMqttChannelAdapter channelAdapter, CancellationToken cancellationToken)
        {
            try
            {
                using (var timeoutToken = new CancellationTokenSource(_options.DefaultCommunicationTimeout))
                using (var effectiveCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(timeoutToken.Token, cancellationToken))
                {
                    var firstPacket = await channelAdapter.ReceivePacketAsync(effectiveCancellationToken.Token)
                        .ConfigureAwait(false);

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
            var context = new ValidatingConnectionEventArgs(connectPacket, channelAdapter)
            {
                SessionItems = new ConcurrentDictionary<object, object>()
            };

            await _eventContainer.ValidatingConnectionEvent.InvokeAsync(context)
                .ConfigureAwait(false);

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
    }
}