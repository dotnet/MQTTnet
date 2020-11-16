using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Implementations;
using MQTTnet.Internal;
using MQTTnet.PacketDispatcher;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server.Status;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public sealed class MqttClientConnection : IDisposable
    {
        readonly Dictionary<ushort, string> _topicAlias = new Dictionary<ushort, string>();
        readonly MqttPacketIdentifierProvider _packetIdentifierProvider = new MqttPacketIdentifierProvider();
        readonly MqttPacketDispatcher _packetDispatcher = new MqttPacketDispatcher();
        readonly CancellationTokenSource _cancellationToken = new CancellationTokenSource();

        readonly IMqttRetainedMessagesManager _retainedMessagesManager;
        readonly MqttClientSessionsManager _sessionsManager;

        readonly IMqttNetScopedLogger _logger;
        readonly IMqttServerOptions _serverOptions;

        readonly IMqttChannelAdapter _channelAdapter;
        readonly MqttConnectionValidatorContext _connectionValidatorContext;
        readonly IMqttDataConverter _dataConverter;
        readonly string _endpoint;
        readonly DateTime _connectedTimestamp;

        volatile Task _packageReceiverTask;
        DateTime _lastNonKeepAlivePacketReceivedTimestamp;

        long _receivedPacketsCount;
        long _sentPacketsCount = 1; // Start with 1 because the CONNECT packet is not counted anywhere.
        long _receivedApplicationMessagesCount;
        long _sentApplicationMessagesCount;
        MqttDisconnectReasonCode _disconnectReason;

        public MqttClientConnection(MqttConnectPacket connectPacket,
            IMqttChannelAdapter channelAdapter,
            MqttClientSession session,
            MqttConnectionValidatorContext connectionValidatorContext,
            IMqttServerOptions serverOptions,
            MqttClientSessionsManager sessionsManager,
            IMqttRetainedMessagesManager retainedMessagesManager,
            IMqttNetLogger logger)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            _serverOptions = serverOptions ?? throw new ArgumentNullException(nameof(serverOptions));
            _sessionsManager = sessionsManager ?? throw new ArgumentNullException(nameof(sessionsManager));
            _retainedMessagesManager = retainedMessagesManager ?? throw new ArgumentNullException(nameof(retainedMessagesManager));

            _channelAdapter = channelAdapter ?? throw new ArgumentNullException(nameof(channelAdapter));
            _connectionValidatorContext = connectionValidatorContext ?? throw new ArgumentNullException(nameof(connectionValidatorContext));
            _dataConverter = _channelAdapter.PacketFormatterAdapter.DataConverter;
            _endpoint = _channelAdapter.Endpoint;
            ConnectPacket = connectPacket ?? throw new ArgumentNullException(nameof(connectPacket));

            if (logger == null) throw new ArgumentNullException(nameof(logger));
            _logger = logger.CreateScopedLogger(nameof(MqttClientConnection));

            _connectedTimestamp = DateTime.UtcNow;
            LastPacketReceivedTimestamp = _connectedTimestamp;
            _lastNonKeepAlivePacketReceivedTimestamp = LastPacketReceivedTimestamp;
        }

        public MqttClientConnectionStatus Status { get; private set; } = MqttClientConnectionStatus.Initializing;

        public MqttConnectPacket ConnectPacket { get; }

        public string ClientId => ConnectPacket.ClientId;

        public bool IsReadingPacket => _channelAdapter.IsReadingPacket;

        public DateTime LastPacketReceivedTimestamp { get; private set; }

        public MqttClientSession Session { get; }

        public async Task StopAsync(MqttDisconnectReasonCode reason)
        {
            Status = MqttClientConnectionStatus.Finalizing;
            _disconnectReason = reason;

            if (reason == MqttDisconnectReasonCode.SessionTakenOver || reason == MqttDisconnectReasonCode.KeepAliveTimeout)
            {
                // Is is very important to send the DISCONNECT packet here BEFORE cancelling the
                // token because the entire connection is closed (disposed) as soon as the cancellation
                // token is cancelled. To there is no chance that the DISCONNECT packet will ever arrive
                // at the client!
                try
                {
                    await _channelAdapter.SendPacketAsync(new MqttDisconnectPacket
                    {
                        ReasonCode = reason
                    }, _serverOptions.DefaultCommunicationTimeout, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    _logger.Warning(exception, "Client '{0}': Error while sending DISCONNECT packet after takeover.", ClientId);
                }
            }

            StopInternal();

            await (_packageReceiverTask ?? PlatformAbstractionLayer.CompletedTask);
        }

        public void ResetStatistics()
        {
            _channelAdapter.ResetStatistics();
        }

        public void FillStatus(MqttClientStatus status)
        {
            status.ClientId = ClientId;
            status.Endpoint = _endpoint;
            status.ProtocolVersion = _channelAdapter.PacketFormatterAdapter.ProtocolVersion;

            status.ReceivedApplicationMessagesCount = Interlocked.Read(ref _receivedApplicationMessagesCount);
            status.SentApplicationMessagesCount = Interlocked.Read(ref _sentApplicationMessagesCount);

            status.ReceivedPacketsCount = Interlocked.Read(ref _receivedPacketsCount);
            status.SentPacketsCount = Interlocked.Read(ref _sentPacketsCount);

            status.ConnectedTimestamp = _connectedTimestamp;
            status.LastPacketReceivedTimestamp = LastPacketReceivedTimestamp;
            status.LastNonKeepAlivePacketReceivedTimestamp = _lastNonKeepAlivePacketReceivedTimestamp;

            status.BytesSent = _channelAdapter.BytesSent;
            status.BytesReceived = _channelAdapter.BytesReceived;
        }

        public void Dispose()
        {
            _cancellationToken.Dispose();
        }

        public Task RunAsync()
        {
            _packageReceiverTask = RunInternalAsync(_cancellationToken.Token);
            return _packageReceiverTask;
        }

        async Task RunInternalAsync(CancellationToken cancellationToken)
        {
            var disconnectType = MqttClientDisconnectType.NotClean;
            try
            {
                _logger.Info("Client '{0}': Session started.", ClientId);

                Session.WillMessage = ConnectPacket.WillMessage;

                await SendAsync(_channelAdapter.PacketFormatterAdapter.DataConverter.CreateConnAckPacket(_connectionValidatorContext), cancellationToken).ConfigureAwait(false);

                Task.Run(() => SendPendingPacketsAsync(cancellationToken), cancellationToken).Forget(_logger);

                Session.IsCleanSession = false;

                while (!cancellationToken.IsCancellationRequested)
                {
                    Status = MqttClientConnectionStatus.Running;

                    var packet = await _channelAdapter.ReceivePacketAsync(cancellationToken).ConfigureAwait(false);
                    if (packet == null)
                    {
                        // The client has closed the connection gracefully.
                        return;
                    }

                    Interlocked.Increment(ref _sentPacketsCount);
                    LastPacketReceivedTimestamp = DateTime.UtcNow;

                    if (!(packet is MqttPingReqPacket || packet is MqttPingRespPacket))
                    {
                        _lastNonKeepAlivePacketReceivedTimestamp = LastPacketReceivedTimestamp;
                    }

                    if (packet is MqttPublishPacket publishPacket)
                    {
                        await HandleIncomingPublishPacketAsync(publishPacket, cancellationToken).ConfigureAwait(false);
                    }
                    else if (packet is MqttPubRelPacket pubRelPacket)
                    {
                        await HandleIncomingPubRelPacketAsync(pubRelPacket, cancellationToken).ConfigureAwait(false);
                    }
                    else if (packet is MqttSubscribePacket subscribePacket)
                    {
                        await HandleIncomingSubscribePacketAsync(subscribePacket, cancellationToken).ConfigureAwait(false);
                    }
                    else if (packet is MqttUnsubscribePacket unsubscribePacket)
                    {
                        await HandleIncomingUnsubscribePacketAsync(unsubscribePacket, cancellationToken).ConfigureAwait(false);
                    }
                    else if (packet is MqttPingReqPacket)
                    {
                        await SendAsync(MqttPingRespPacket.Instance, cancellationToken).ConfigureAwait(false);
                    }
                    else if (packet is MqttDisconnectPacket)
                    {
                        Session.WillMessage = null;
                        disconnectType = MqttClientDisconnectType.Clean;

                        StopInternal();
                        return;
                    }
                    else
                    {
                        _packetDispatcher.Dispatch(packet);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                if (exception is MqttCommunicationException)
                {
                    _logger.Warning(exception, "Client '{0}': Communication exception while receiving client packets.", ClientId);
                }
                else
                {
                    _logger.Error(exception, "Client '{0}': Error while receiving client packets.", ClientId);
                }

                StopInternal();
            }
            finally
            {
                if (_disconnectReason == MqttDisconnectReasonCode.SessionTakenOver)
                {
                    disconnectType = MqttClientDisconnectType.Takeover;
                }

                if (Session.WillMessage != null)
                {
                    _sessionsManager.DispatchApplicationMessage(Session.WillMessage, this);
                    Session.WillMessage = null;
                }

                _packetDispatcher.Cancel();

                _logger.Info("Client '{0}': Connection stopped.", ClientId);

                try
                {
                    await _sessionsManager.CleanUpClient(ClientId, _channelAdapter, disconnectType);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Client '{0}': Error while cleaning up", ClientId);
                }
            }
        }

        void StopInternal()
        {
            _cancellationToken.Cancel();
        }

        async Task EnqueueSubscribedRetainedMessagesAsync(ICollection<MqttTopicFilter> topicFilters)
        {
            var retainedMessages = await _retainedMessagesManager.GetSubscribedMessagesAsync(topicFilters).ConfigureAwait(false);
            foreach (var applicationMessage in retainedMessages)
            {
                Session.EnqueueApplicationMessage(applicationMessage, ClientId, true);
            }
        }

        Task HandleIncomingPubRelPacketAsync(MqttPubRelPacket pubRelPacket, CancellationToken cancellationToken)
        {
            var pubCompPacket = new MqttPubCompPacket
            {
                PacketIdentifier = pubRelPacket.PacketIdentifier,
                ReasonCode = MqttPubCompReasonCode.Success
            };

            return SendAsync(pubCompPacket, cancellationToken);
        }

        async Task HandleIncomingSubscribePacketAsync(MqttSubscribePacket subscribePacket, CancellationToken cancellationToken)
        {
            var subscribeResult = await Session.SubscriptionsManager.SubscribeAsync(subscribePacket, ConnectPacket).ConfigureAwait(false);
            var subAckPacket = _channelAdapter.PacketFormatterAdapter.DataConverter.CreateSubAckPacket(subscribePacket, subscribeResult);

            await SendAsync(subAckPacket, cancellationToken).ConfigureAwait(false);

            if (subscribeResult.CloseConnection)
            {
                StopInternal();
                return;
            }

            await EnqueueSubscribedRetainedMessagesAsync(subscribePacket.TopicFilters).ConfigureAwait(false);
        }

        async Task HandleIncomingUnsubscribePacketAsync(MqttUnsubscribePacket unsubscribePacket, CancellationToken cancellationToken)
        {
            var reasonCodes = await Session.SubscriptionsManager.UnsubscribeAsync(unsubscribePacket).ConfigureAwait(false);
            var unsubAckPacket = _channelAdapter.PacketFormatterAdapter.DataConverter.CreateUnsubAckPacket(unsubscribePacket, reasonCodes);

            await SendAsync(unsubAckPacket, cancellationToken).ConfigureAwait(false);
        }

        Task HandleIncomingPublishPacketAsync(MqttPublishPacket publishPacket, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _sentApplicationMessagesCount);

            HandleTopicAlias(publishPacket);

            var applicationMessage = _dataConverter.CreateApplicationMessage(publishPacket);
            _sessionsManager.DispatchApplicationMessage(applicationMessage, this);

            switch (publishPacket.QualityOfServiceLevel)
            {
                case MqttQualityOfServiceLevel.AtMostOnce:
                    {
                        return PlatformAbstractionLayer.CompletedTask;
                    }
                case MqttQualityOfServiceLevel.AtLeastOnce:
                    {
                        var pubAckPacket = _dataConverter.CreatePubAckPacket(publishPacket);
                        return SendAsync(pubAckPacket, cancellationToken);
                    }
                case MqttQualityOfServiceLevel.ExactlyOnce:
                    {
                        var pubRecPacket = _dataConverter.CreatePubRecPacket(publishPacket);
                        return SendAsync(pubRecPacket, cancellationToken);
                    }
                default:
                    {
                        throw new MqttCommunicationException("Received a not supported QoS level.");
                    }
            }
        }

        void HandleTopicAlias(MqttPublishPacket publishPacket)
        {
            if (publishPacket.Properties?.TopicAlias == null)
            {
                return;
            }

            var topicAlias = publishPacket.Properties.TopicAlias.Value;

            lock (_topicAlias)
            {
                if (!string.IsNullOrEmpty(publishPacket.Topic))
                {
                    _topicAlias[topicAlias] = publishPacket.Topic;
                }
                else
                {
                    if (_topicAlias.TryGetValue(topicAlias, out var topic))
                    {
                        publishPacket.Topic = topic;
                    }
                    else
                    {

                    }
                }
            }
        }

        async Task SendPendingPacketsAsync(CancellationToken cancellationToken)
        {
            MqttQueuedApplicationMessage queuedApplicationMessage = null;
            MqttPublishPacket publishPacket = null;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    queuedApplicationMessage = await Session.ApplicationMessagesQueue.DequeueAsync(cancellationToken).ConfigureAwait(false);
                    if (queuedApplicationMessage == null)
                    {
                        return;
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    publishPacket = _dataConverter.CreatePublishPacket(queuedApplicationMessage.ApplicationMessage);
                    publishPacket.QualityOfServiceLevel = queuedApplicationMessage.SubscriptionQualityOfServiceLevel;

                    // Set the retain flag to true according to [MQTT-3.3.1-8] and [MQTT-3.3.1-9].
                    publishPacket.Retain = queuedApplicationMessage.IsRetainedMessage;

                    if (_serverOptions.ClientMessageQueueInterceptor != null)
                    {
                        var context = new MqttClientMessageQueueInterceptorContext(
                            queuedApplicationMessage.SenderClientId,
                            ClientId,
                            queuedApplicationMessage.ApplicationMessage,
                            queuedApplicationMessage.SubscriptionQualityOfServiceLevel);

                        if (_serverOptions.ClientMessageQueueInterceptor != null)
                        {
                            await _serverOptions.ClientMessageQueueInterceptor.InterceptClientMessageQueueEnqueueAsync(context).ConfigureAwait(false);
                        }

                        if (!context.AcceptEnqueue || context.ApplicationMessage == null)
                        {
                            return;
                        }

                        publishPacket.Topic = context.ApplicationMessage.Topic;
                        publishPacket.Payload = context.ApplicationMessage.Payload;
                        publishPacket.QualityOfServiceLevel = context.SubscriptionQualityOfServiceLevel;
                    }

                    if (publishPacket.QualityOfServiceLevel > 0)
                    {
                        publishPacket.PacketIdentifier = _packetIdentifierProvider.GetNextPacketIdentifier();
                    }

                    if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtMostOnce)
                    {
                        await SendAsync(publishPacket, cancellationToken).ConfigureAwait(false);
                    }
                    else if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtLeastOnce)
                    {
                        var awaiter = _packetDispatcher.AddAwaiter<MqttPubAckPacket>(publishPacket.PacketIdentifier);
                        await SendAsync(publishPacket, cancellationToken).ConfigureAwait(false);
                        await awaiter.WaitOneAsync(_serverOptions.DefaultCommunicationTimeout).ConfigureAwait(false);
                    }
                    else if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.ExactlyOnce)
                    {
                        using (var awaiter1 = _packetDispatcher.AddAwaiter<MqttPubRecPacket>(publishPacket.PacketIdentifier))
                        using (var awaiter2 = _packetDispatcher.AddAwaiter<MqttPubCompPacket>(publishPacket.PacketIdentifier))
                        {
                            await SendAsync(publishPacket, cancellationToken).ConfigureAwait(false);
                            await awaiter1.WaitOneAsync(_serverOptions.DefaultCommunicationTimeout).ConfigureAwait(false);

                            await SendAsync(new MqttPubRelPacket
                            {
                                PacketIdentifier = publishPacket.PacketIdentifier,
                                ReasonCode = MqttPubRelReasonCode.Success
                            }, cancellationToken).ConfigureAwait(false);

                            await awaiter2.WaitOneAsync(_serverOptions.DefaultCommunicationTimeout).ConfigureAwait(false);
                        }
                    }

                    _logger.Verbose("Client '{0}': Queued application message sent.", ClientId);
                }
            }
            catch (Exception exception)
            {
                if (exception is OperationCanceledException)
                {
                }
                else if (exception is MqttCommunicationTimedOutException)
                {
                    _logger.Warning(exception, "Client '{0}': Sending publish packet failed: Timeout.", ClientId);
                }
                else if (exception is MqttCommunicationException)
                {
                    _logger.Warning(exception, "Client '{0}': Sending publish packet failed: Communication exception.", ClientId);
                }
                else
                {
                    _logger.Error(exception, "Client '{0}': Sending publish packet failed.", ClientId);
                }

                if (publishPacket?.QualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce)
                {
                    queuedApplicationMessage.IsDuplicate = true;

                    Session.ApplicationMessagesQueue.Enqueue(queuedApplicationMessage);
                }

                StopInternal();
            }
        }

        async Task SendAsync(MqttBasePacket packet, CancellationToken cancellationToken)
        {
            await _channelAdapter.SendPacketAsync(packet, _serverOptions.DefaultCommunicationTimeout, cancellationToken).ConfigureAwait(false);

            Interlocked.Increment(ref _receivedPacketsCount);

            if (packet is MqttPublishPacket)
            {
                Interlocked.Increment(ref _receivedApplicationMessagesCount);
            }
        }
    }
}
