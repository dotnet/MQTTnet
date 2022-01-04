using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Implementations;
using MQTTnet.Internal;
using MQTTnet.PacketDispatcher;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server.Status;

namespace MQTTnet.Server.Internal
{
    public sealed class MqttClientConnection
    {
        readonly Dictionary<ushort, string> _topicAlias = new Dictionary<ushort, string>();
        readonly MqttPacketIdentifierProvider _packetIdentifierProvider = new MqttPacketIdentifierProvider();
        readonly MqttPacketDispatcher _packetDispatcher = new MqttPacketDispatcher();
        
        readonly MqttClientSessionsManager _sessionsManager;

        readonly IMqttChannelAdapter _channelAdapter;
        readonly MqttNetSourceLogger _logger;
        readonly IMqttServerOptions _serverOptions;

        readonly string _endpoint;
        readonly MqttConnectPacket _connectPacket;

        CancellationTokenSource _cancellationToken;

        public MqttClientConnection(
            MqttConnectPacket connectPacket,
            IMqttChannelAdapter channelAdapter,
            MqttClientSession session,
            IMqttServerOptions serverOptions,
            MqttClientSessionsManager sessionsManager,
            IMqttNetLogger logger)
        {
            _serverOptions = serverOptions ?? throw new ArgumentNullException(nameof(serverOptions));
            _sessionsManager = sessionsManager ?? throw new ArgumentNullException(nameof(sessionsManager));
            
            _channelAdapter = channelAdapter ?? throw new ArgumentNullException(nameof(channelAdapter));
            _endpoint = channelAdapter.Endpoint;
            
            Session = session ?? throw new ArgumentNullException(nameof(session));
            _connectPacket = connectPacket ?? throw new ArgumentNullException(nameof(connectPacket));

            if (logger == null) throw new ArgumentNullException(nameof(logger));
            _logger = logger.WithSource(nameof(MqttClientConnection));
        }

        public string ClientId => _connectPacket.ClientId;

        public string Endpoint => _endpoint;

        public MqttClientConnectionStatistics Statistics { get; } = new MqttClientConnectionStatistics();

        public bool IsRunning { get; private set; }

        public ushort KeepAlivePeriod => _connectPacket.KeepAlivePeriod;

        public bool IsReadingPacket => _channelAdapter.IsReadingPacket;

        public MqttClientSession Session { get; }

        public bool IsTakenOver { get; set; }

        public bool IsCleanDisconnect { get; private set; }

        public async Task StopAsync(MqttClientDisconnectReason reason)
        {
            IsRunning = false;

            if (reason == MqttClientDisconnectReason.SessionTakenOver || reason == MqttClientDisconnectReason.KeepAliveTimeout)
            {
                // Is is very important to send the DISCONNECT packet here BEFORE cancelling the
                // token because the entire connection is closed (disposed) as soon as the cancellation
                // token is cancelled. To there is no chance that the DISCONNECT packet will ever arrive
                // at the client!
                await TrySendDisconnectPacket(reason).ConfigureAwait(false);
            }

            StopInternal();
        }

        public void ResetStatistics()
        {
            _channelAdapter.ResetStatistics();
        }

        public void FillClientStatus(MqttClientStatus clientStatus)
        {
            clientStatus.ClientId = ClientId;
            clientStatus.Endpoint = _endpoint;

            clientStatus.ProtocolVersion = _channelAdapter.PacketFormatterAdapter.ProtocolVersion;
            clientStatus.BytesSent = _channelAdapter.BytesSent;
            clientStatus.BytesReceived = _channelAdapter.BytesReceived;

            Statistics.FillClientStatus(clientStatus);
        }

        public async Task RunAsync()
        {
            _logger.Info("Client '{0}': Session started.", ClientId);

            Session.WillMessage = _connectPacket.WillMessage;
            
            using (var cancellationToken = new CancellationTokenSource())
            {
                _cancellationToken = cancellationToken;

                try
                {
                    Task.Run(() => SendPacketsLoop(cancellationToken.Token), cancellationToken.Token).RunInBackground(_logger);

                    Session.IsCleanSession = false;

                    IsRunning = true;

                    await ReceivePackagesLoop(cancellationToken.Token).ConfigureAwait(false);
                }
                finally
                {
                    IsRunning = false;
                    
                    cancellationToken.Cancel();
                    _cancellationToken = null;
                }
            }
        
            _packetDispatcher.CancelAll();

            if (!IsTakenOver && !IsCleanDisconnect && Session.WillMessage != null)
            {
                _sessionsManager.DispatchApplicationMessage(Session.WillMessage, this);
                Session.WillMessage = null;
            }

            _logger.Info("Client '{0}': Connection stopped.", ClientId);
        }

        Task SendPacketAsync(MqttBasePacket packet, CancellationToken cancellationToken)
        {
            return _channelAdapter.SendPacketAsync(packet, cancellationToken).ContinueWith(task => { Statistics.HandleSentPacket(packet); }, cancellationToken);
        }

        async Task ReceivePackagesLoop(CancellationToken cancellationToken)
        {
            try
            {
                // We do not listen for the cancellation token here because the internal buffer might still
                // contain data to be read even if the TCP connection was already dropped. So we rely on an
                // own exception in the reading loop!
                while (!cancellationToken.IsCancellationRequested)
                {
                    var packet = await _channelAdapter.ReceivePacketAsync(cancellationToken).ConfigureAwait(false);
                    if (packet == null)
                    {
                        // The client has closed the connection gracefully.
                        return;
                    }

                    Statistics.HandleReceivedPacket(packet);

                    if (packet is MqttPublishPacket publishPacket)
                    {
                        await HandleIncomingPublishPacket(publishPacket, cancellationToken).ConfigureAwait(false);
                    }
                    else if (packet is MqttPubRelPacket pubRelPacket)
                    {
                        await HandleIncomingPubRelPacket(pubRelPacket, cancellationToken).ConfigureAwait(false);
                    }
                    else if (packet is MqttSubscribePacket subscribePacket)
                    {
                        await HandleIncomingSubscribePacket(subscribePacket, cancellationToken).ConfigureAwait(false);
                    }
                    else if (packet is MqttUnsubscribePacket unsubscribePacket)
                    {
                        await HandleIncomingUnsubscribePacket(unsubscribePacket, cancellationToken).ConfigureAwait(false);
                    }
                    else if (packet is MqttPingReqPacket)
                    {
                        // See: The Server MUST send a PINGRESP packet in response to a PINGREQ packet [MQTT-3.12.4-1].
                        await SendPacketAsync(MqttPingRespPacket.Instance, cancellationToken).ConfigureAwait(false);
                    }
                    else if (packet is MqttPingRespPacket)
                    {
                        throw new MqttProtocolViolationException("A PINGRESP Packet is sent by the Server to the Client in response to a PINGREQ Packet only.");
                    }
                    else if (packet is MqttDisconnectPacket)
                    {
                        IsCleanDisconnect = true;
                        return;
                    }
                    else
                    {
                        if (!_packetDispatcher.TryDispatch(packet))
                        {
                            throw new MqttProtocolViolationException($"Received packet '{packet}' at an unexpected time.");
                        }
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
            }
        }

        async Task SendPacketsLoop(CancellationToken cancellationToken)
        {
            MqttQueuedApplicationMessage queuedApplicationMessage = null;
            MqttPublishPacket publishPacket = null;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    queuedApplicationMessage = await Session.ApplicationMessagesQueue.Dequeue(cancellationToken).ConfigureAwait(false);
                    
                    // Also check the cancellation token here because the dequeue is blocking and may take some time.
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    
                    if (queuedApplicationMessage == null)
                    {
                        continue;
                    }

                    publishPacket = await CreatePublishPacket(queuedApplicationMessage).ConfigureAwait(false);
                    if (publishPacket == null)
                    {
                        continue;
                    }

                    if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtMostOnce)
                    {
                        await SendPacketAsync(publishPacket, cancellationToken).ConfigureAwait(false);
                    }
                    else if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtLeastOnce)
                    {
                        using (var awaitable = _packetDispatcher.AddAwaitable<MqttPubAckPacket>(publishPacket.PacketIdentifier))
                        {
                            await SendPacketAsync(publishPacket, cancellationToken).ConfigureAwait(false);
                            await awaitable.WaitOneAsync(_serverOptions.DefaultCommunicationTimeout).ConfigureAwait(false);
                        }
                    }
                    else if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.ExactlyOnce)
                    {
                        using (var awaitableRec = _packetDispatcher.AddAwaitable<MqttPubRecPacket>(publishPacket.PacketIdentifier))
                        using (var awaitableComp = _packetDispatcher.AddAwaitable<MqttPubCompPacket>(publishPacket.PacketIdentifier))
                        {
                            await SendPacketAsync(publishPacket, cancellationToken).ConfigureAwait(false);
                            var pubRecPacket = await awaitableRec.WaitOneAsync(_serverOptions.DefaultCommunicationTimeout).ConfigureAwait(false);

                            var pubRelPacket = _channelAdapter.PacketFormatterAdapter.DataConverter.CreatePubRelPacket(pubRecPacket, MqttApplicationMessageReceivedReasonCode.Success);
                            await SendPacketAsync(pubRelPacket, cancellationToken).ConfigureAwait(false);

                            await awaitableComp.WaitOneAsync(_serverOptions.DefaultCommunicationTimeout).ConfigureAwait(false);
                        }
                    }

                    if (_logger.IsEnabled)
                    {
                        _logger.Verbose("Client '{0}': Queued application message sent.", ClientId);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                if (exception is MqttCommunicationTimedOutException)
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
                    if (queuedApplicationMessage != null)
                    {
                        queuedApplicationMessage.IsDuplicate = true;
                        Session.ApplicationMessagesQueue.Enqueue(queuedApplicationMessage);
                    }
                }

                StopInternal();
            }
        }

        async Task<MqttPublishPacket> CreatePublishPacket(MqttQueuedApplicationMessage queuedApplicationMessage)
        {
            var publishPacket = _channelAdapter.PacketFormatterAdapter.DataConverter.CreatePublishPacket(queuedApplicationMessage.ApplicationMessage);
            publishPacket.QualityOfServiceLevel = queuedApplicationMessage.SubscriptionQualityOfServiceLevel;

            if (_channelAdapter.PacketFormatterAdapter.ProtocolVersion == MqttProtocolVersion.V500)
            {
                publishPacket.Properties.SubscriptionIdentifiers = queuedApplicationMessage.SubscriptionIdentifiers;
            }

            // Set the retain flag to true according to [MQTT-3.3.1-8] and [MQTT-3.3.1-9].
            publishPacket.Retain = queuedApplicationMessage.IsRetainedMessage;

            publishPacket = await InvokeClientMessageQueueInterceptor(publishPacket, queuedApplicationMessage).ConfigureAwait(false);
            if (publishPacket == null)
            {
                // The interceptor has decided that the message is not relevant and will be fully ignored.
                return null;
            }

            if (publishPacket.QualityOfServiceLevel > 0)
            {
                publishPacket.PacketIdentifier = _packetIdentifierProvider.GetNextPacketIdentifier();
            }

            return publishPacket;
        }

        void StopInternal()
        {
            _cancellationToken?.Cancel();
        }
        
        Task HandleIncomingPubRelPacket(MqttPubRelPacket pubRelPacket, CancellationToken cancellationToken)
        {
            var pubCompPacket = _channelAdapter.PacketFormatterAdapter.DataConverter.CreatePubCompPacket(pubRelPacket, MqttApplicationMessageReceivedReasonCode.Success);
            return SendPacketAsync(pubCompPacket, cancellationToken);
        }

        async Task HandleIncomingSubscribePacket(MqttSubscribePacket subscribePacket, CancellationToken cancellationToken)
        {
            var subscribeResult = await Session.SubscriptionsManager.Subscribe(subscribePacket).ConfigureAwait(false);
            var subAckPacket = _channelAdapter.PacketFormatterAdapter.DataConverter.CreateSubAckPacket(subscribePacket, subscribeResult);

            await SendPacketAsync(subAckPacket, cancellationToken).ConfigureAwait(false);

            if (subscribeResult.CloseConnection)
            {
                StopInternal();
                return;
            }
            
            foreach (var retainedApplicationMessage in subscribeResult.RetainedApplicationMessages)
            {
                Session.ApplicationMessagesQueue.Enqueue(retainedApplicationMessage);
            }
        }

        async Task HandleIncomingUnsubscribePacket(MqttUnsubscribePacket unsubscribePacket, CancellationToken cancellationToken)
        {
            var reasonCodes = await Session.SubscriptionsManager.Unsubscribe(unsubscribePacket).ConfigureAwait(false);
            var unsubAckPacket = _channelAdapter.PacketFormatterAdapter.DataConverter.CreateUnsubAckPacket(unsubscribePacket, reasonCodes);

            await SendPacketAsync(unsubAckPacket, cancellationToken).ConfigureAwait(false);
        }

        Task HandleIncomingPublishPacket(MqttPublishPacket publishPacket, CancellationToken cancellationToken)
        {
            HandleTopicAlias(publishPacket);

            var applicationMessage = _channelAdapter.PacketFormatterAdapter.DataConverter.CreateApplicationMessage(publishPacket);
            _sessionsManager.DispatchApplicationMessage(applicationMessage, this);

            switch (publishPacket.QualityOfServiceLevel)
            {
                case MqttQualityOfServiceLevel.AtMostOnce:
                {
                    return PlatformAbstractionLayer.CompletedTask;
                }
                case MqttQualityOfServiceLevel.AtLeastOnce:
                {
                    var pubAckPacket = _channelAdapter.PacketFormatterAdapter.DataConverter.CreatePubAckPacket(publishPacket, MqttApplicationMessageReceivedReasonCode.Success);
                    return SendPacketAsync(pubAckPacket, cancellationToken);
                }
                case MqttQualityOfServiceLevel.ExactlyOnce:
                {
                    var pubRecPacket = _channelAdapter.PacketFormatterAdapter.DataConverter.CreatePubRecPacket(publishPacket, MqttApplicationMessageReceivedReasonCode.Success);
                    return SendPacketAsync(pubRecPacket, cancellationToken);
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
        
        async Task TrySendDisconnectPacket(MqttClientDisconnectReason reason)
        {
            try
            {
                var disconnectOptions = new MqttClientDisconnectOptions
                {
                    ReasonCode = reason,
                    ReasonString = reason.ToString()
                };

                var disconnectPacket = _channelAdapter.PacketFormatterAdapter.DataConverter.CreateDisconnectPacket(disconnectOptions);

                using (var timeout = new CancellationTokenSource(_serverOptions.DefaultCommunicationTimeout))
                {
                    await SendPacketAsync(disconnectPacket, timeout.Token).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                _logger.Warning(exception, "Client '{{0}}': Error while sending DISCONNECT packet (Reason = {1}).", ClientId, reason);
            }
        }

        async Task<MqttPublishPacket> InvokeClientMessageQueueInterceptor(MqttPublishPacket publishPacket, MqttQueuedApplicationMessage queuedApplicationMessage)
        {
            if (_serverOptions.ClientMessageQueueInterceptor == null)
            {
                return publishPacket;
            }

            var context = new MqttClientMessageQueueInterceptorContext
            {
                SenderClientId = queuedApplicationMessage.SenderClientId,
                ReceiverClientId = ClientId,
                ApplicationMessage = queuedApplicationMessage.ApplicationMessage,
                SubscriptionQualityOfServiceLevel = queuedApplicationMessage.SubscriptionQualityOfServiceLevel
            };

            if (_serverOptions.ClientMessageQueueInterceptor != null)
            {
                await _serverOptions.ClientMessageQueueInterceptor.InterceptClientMessageQueueEnqueueAsync(context).ConfigureAwait(false);
            }

            if (!context.AcceptEnqueue || context.ApplicationMessage == null)
            {
                return null;
            }

            publishPacket.Topic = context.ApplicationMessage.Topic;
            publishPacket.Payload = context.ApplicationMessage.Payload;
            publishPacket.QualityOfServiceLevel = context.SubscriptionQualityOfServiceLevel;

            return publishPacket;
        }
    }
}