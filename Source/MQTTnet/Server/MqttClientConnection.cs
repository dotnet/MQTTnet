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

        volatile bool _isTakeover;

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

        public MqttConnectPacket ConnectPacket { get; }

        public string ClientId => ConnectPacket.ClientId;

        public bool IsReadingPacket => _channelAdapter.IsReadingPacket;

        public DateTime LastPacketReceivedTimestamp { get; private set; }

        public MqttClientSession Session { get; }

        public Task StopAsync(bool isTakeover = false)
        {
            _isTakeover = isTakeover;
            var task = _packageReceiverTask;

            StopInternal();

            if (task != null)
            {
                return task;
            }

            return PlatformAbstractionLayer.CompletedTask;
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
            _packageReceiverTask = RunInternalAsync();
            return _packageReceiverTask;
        }

        async Task RunInternalAsync()
        {
            var disconnectType = MqttClientDisconnectType.NotClean;
            try
            {
                _logger.Info("Client '{0}': Session started.", ClientId);

                Session.WillMessage = ConnectPacket.WillMessage;

                var cancellationToken = _cancellationToken.Token;

                Task.Run(() => SendPendingPacketsAsync(cancellationToken), cancellationToken).Forget(_logger);

                await SendAsync(_channelAdapter.PacketFormatterAdapter.DataConverter.CreateConnAckPacket(_connectionValidatorContext), cancellationToken).ConfigureAwait(false);

                Session.IsCleanSession = false;

                while (!cancellationToken.IsCancellationRequested)
                {
                    var packet = await _channelAdapter.ReceivePacketAsync(TimeSpan.Zero, cancellationToken).ConfigureAwait(false);
                    if (packet == null)
                    {
                        // The client has closed the connection gracefully.
                        break;
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
                        continue;
                    }

                    if (packet is MqttPubRelPacket pubRelPacket)
                    {
                        var pubCompPacket = new MqttPubCompPacket
                        {
                            PacketIdentifier = pubRelPacket.PacketIdentifier,
                            ReasonCode = MqttPubCompReasonCode.Success
                        };

                        await SendAsync(pubCompPacket, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    if (packet is MqttSubscribePacket subscribePacket)
                    {
                        await HandleIncomingSubscribePacketAsync(subscribePacket, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    if (packet is MqttUnsubscribePacket unsubscribePacket)
                    {
                        await HandleIncomingUnsubscribePacketAsync(unsubscribePacket, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    if (packet is MqttPingReqPacket)
                    {
                        await SendAsync(MqttPingRespPacket.Instance, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    if (packet is MqttDisconnectPacket)
                    {
                        Session.WillMessage = null;
                        disconnectType = MqttClientDisconnectType.Clean;

                        StopInternal();
                        break;
                    }

                    _packetDispatcher.Dispatch(packet);
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
                if (_isTakeover)
                {
                    disconnectType = MqttClientDisconnectType.Takeover;
                }
                
                if (Session.WillMessage != null)
                {
                    _sessionsManager.DispatchApplicationMessage(Session.WillMessage, this);
                    Session.WillMessage = null;
                }

                _packetDispatcher.Reset();

                _packageReceiverTask = null;

                if (_isTakeover)
                {
                    try
                    {
                        // Don't use SendAsync here _cancellationToken is already cancelled.
                        await _channelAdapter.SendPacketAsync(new MqttDisconnectPacket
                        {
                            ReasonCode = MqttDisconnectReasonCode.SessionTakenOver
                        }, TimeSpan.Zero, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        _logger.Error(exception, "Client '{0}': Error while sending DISCONNECT packet after takeover.", ClientId);
                    }
                }

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
            _cancellationToken.Cancel(false);
        }

        async Task EnqueueSubscribedRetainedMessagesAsync(ICollection<MqttTopicFilter> topicFilters)
        {
            var retainedMessages = await _retainedMessagesManager.GetSubscribedMessagesAsync(topicFilters).ConfigureAwait(false);
            foreach (var applicationMessage in retainedMessages)
            {
                Session.EnqueueApplicationMessage(applicationMessage, ClientId, true);
            }
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

            switch (publishPacket.QualityOfServiceLevel)
            {
                case MqttQualityOfServiceLevel.AtMostOnce:
                    {
                        return HandleIncomingPublishPacketWithQoS0Async(publishPacket);
                    }
                case MqttQualityOfServiceLevel.AtLeastOnce:
                    {
                        return HandleIncomingPublishPacketWithQoS1Async(publishPacket, cancellationToken);
                    }
                case MqttQualityOfServiceLevel.ExactlyOnce:
                    {
                        return HandleIncomingPublishPacketWithQoS2Async(publishPacket, cancellationToken);
                    }
                default:
                    {
                        throw new MqttCommunicationException("Received a not supported QoS level.");
                    }
            }
        }

        Task HandleIncomingPublishPacketWithQoS0Async(MqttPublishPacket publishPacket)
        {
            var applicationMessage = _dataConverter.CreateApplicationMessage(publishPacket);

            _sessionsManager.DispatchApplicationMessage(applicationMessage, this);

            return PlatformAbstractionLayer.CompletedTask;
        }

        Task HandleIncomingPublishPacketWithQoS1Async(MqttPublishPacket publishPacket, CancellationToken cancellationToken)
        {
            var applicationMessage = _dataConverter.CreateApplicationMessage(publishPacket);
            _sessionsManager.DispatchApplicationMessage(applicationMessage, this);

            var pubAckPacket = _dataConverter.CreatePubAckPacket(publishPacket);
            return SendAsync(pubAckPacket, cancellationToken);
        }

        Task HandleIncomingPublishPacketWithQoS2Async(MqttPublishPacket publishPacket, CancellationToken cancellationToken)
        {
            var applicationMessage = _dataConverter.CreateApplicationMessage(publishPacket);
            _sessionsManager.DispatchApplicationMessage(applicationMessage, this);

            var pubRecPacket = new MqttPubRecPacket
            {
                PacketIdentifier = publishPacket.PacketIdentifier,
                ReasonCode = MqttPubRecReasonCode.Success
            };

            return SendAsync(pubRecPacket, cancellationToken);
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

                            await SendAsync(new MqttPubRelPacket { PacketIdentifier = publishPacket.PacketIdentifier }, cancellationToken).ConfigureAwait(false);
                            await awaiter2.WaitOneAsync(_serverOptions.DefaultCommunicationTimeout).ConfigureAwait(false);
                        }
                    }

                    _logger.Verbose("Queued application message sent (ClientId: {0}).", ClientId);
                }
            }
            catch (Exception exception)
            {
                if (exception is MqttCommunicationTimedOutException)
                {
                    _logger.Warning(exception, "Sending publish packet failed: Timeout (ClientId: {0}).", ClientId);
                }
                else if (exception is MqttCommunicationException)
                {
                    _logger.Warning(exception, "Sending publish packet failed: Communication exception (ClientId: {0}).", ClientId);
                }
                else if (exception is OperationCanceledException)
                {
                }
                else
                {
                    _logger.Error(exception, "Sending publish packet failed (ClientId: {0}).", ClientId);
                }

                if (publishPacket?.QualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce)
                {
                    queuedApplicationMessage.IsDuplicate = true;

                    Session.ApplicationMessagesQueue.Enqueue(queuedApplicationMessage);
                }

                if (!_cancellationToken.Token.IsCancellationRequested)
                {
                    await StopAsync().ConfigureAwait(false);
                }
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
