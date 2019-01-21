using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.MessageStream;
using MQTTnet.PacketDispatcher;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public class MqttClientSession : IMqttClientSession
    {
        private readonly MqttPacketIdentifierProvider _packetIdentifierProvider = new MqttPacketIdentifierProvider();
        private readonly MqttPacketDispatcher _packetDispatcher = new MqttPacketDispatcher();

        private readonly MqttRetainedMessagesManager _retainedMessagesManager;
        private readonly MqttServerEventDispatcher _eventDispatcher;
        private readonly MqttClientKeepAliveMonitor _keepAliveMonitor;
        private readonly MqttClientSessionPendingMessagesQueue _pendingMessagesQueue;
        private readonly MqttClientSubscriptionsManager _subscriptionsManager;
        private readonly MqttClientSessionsManager _sessionsManager;

        private readonly IMqttNetChildLogger _logger;
        private readonly IMqttServerOptions _options;

        private CancellationTokenSource _cancellationTokenSource;
        private MqttApplicationMessage _willMessage;
        private bool _wasCleanDisconnect;
        private Task _workerTask;
        private IMqttChannelAdapter _channelAdapter;

        private long _receivedMessagesCount;
        private bool _isCleanSession = true;

        public MqttClientSession(
            string clientId,
            IMqttServerOptions options,
            MqttClientSessionsManager sessionsManager,
            MqttRetainedMessagesManager retainedMessagesManager,
            MqttServerEventDispatcher eventDispatcher,
            IMqttNetChildLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            _options = options ?? throw new ArgumentNullException(nameof(options));
            _sessionsManager = sessionsManager ?? throw new ArgumentNullException(nameof(sessionsManager));
            _retainedMessagesManager = retainedMessagesManager ?? throw new ArgumentNullException(nameof(retainedMessagesManager));
            _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));

            ClientId = clientId;

            _logger = logger.CreateChildLogger(nameof(MqttClientSession));

            _keepAliveMonitor = new MqttClientKeepAliveMonitor(this, _logger);
            _subscriptionsManager = new MqttClientSubscriptionsManager(clientId, _options, eventDispatcher);
            _pendingMessagesQueue = new MqttClientSessionPendingMessagesQueue(_options, this, _packetDispatcher, _logger);
        }

        public string ClientId { get; }

        public void FillStatus(MqttClientSessionStatus status)
        {
            status.ClientId = ClientId;
            status.IsConnected = _cancellationTokenSource != null;
            status.Endpoint = _channelAdapter?.Endpoint;
            status.ProtocolVersion = _channelAdapter?.PacketFormatterAdapter?.ProtocolVersion;
            status.PendingApplicationMessagesCount = _pendingMessagesQueue.Count;
            status.ReceivedApplicationMessagesCount = _pendingMessagesQueue.SentMessagesCount;
            status.SentApplicationMessagesCount = Interlocked.Read(ref _receivedMessagesCount);
            status.LastPacketReceived = _keepAliveMonitor.LastPacketReceived;
            status.LastNonKeepAlivePacketReceived = _keepAliveMonitor.LastNonKeepAlivePacketReceived;
        }

        public async Task StopAsync(MqttClientDisconnectType type)
        {
            StopInternal(type);

            var task = _workerTask;
            if (task != null && !task.IsCompleted)
            {
                await task.ConfigureAwait(false);
            }
        }

        public async Task SubscribeAsync(IEnumerable<TopicFilter> topicFilters)
        {
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            var topicFiltersCollection = topicFilters.ToList();

            var packet = new MqttSubscribePacket();
            packet.TopicFilters.AddRange(topicFiltersCollection);

            await _subscriptionsManager.SubscribeAsync(packet).ConfigureAwait(false);
            await EnqueueSubscribedRetainedMessagesAsync(topicFiltersCollection).ConfigureAwait(false);
        }

        public Task UnsubscribeAsync(IEnumerable<string> topicFilters)
        {
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            var packet = new MqttUnsubscribePacket();
            packet.TopicFilters.AddRange(topicFilters);

            _subscriptionsManager.Unsubscribe(packet);

            return Task.FromResult(0);
        }

        public void ClearPendingApplicationMessages()
        {
            _pendingMessagesQueue.Clear();
        }

        public void Dispose()
        {
            _pendingMessagesQueue?.Dispose();

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        public Task RunAsync(MqttConnectPacket connectPacket, IMqttChannelAdapter channelAdapter)
        {
            if (connectPacket == null) throw new ArgumentNullException(nameof(connectPacket));
            if (channelAdapter == null) throw new ArgumentNullException(nameof(channelAdapter));

            _workerTask = RunInternalAsync(connectPacket, channelAdapter);
            return _workerTask;
        }

        public async Task EnqueueApplicationMessageAsync(MqttClientSession senderClientSession, MqttApplicationMessage applicationMessage, bool isRetainedApplicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            var checkSubscriptionsResult = _subscriptionsManager.CheckSubscriptions(applicationMessage.Topic, applicationMessage.QualityOfServiceLevel);
            if (!checkSubscriptionsResult.IsSubscribed)
            {
                return;
            }

            var publishPacket = _channelAdapter.PacketFormatterAdapter.DataConverter.CreatePublishPacket(applicationMessage);
            publishPacket.QualityOfServiceLevel = checkSubscriptionsResult.QualityOfServiceLevel;

            // Set the retain flag to true according to [MQTT-3.3.1-8] and [MQTT-3.3.1-9].
            publishPacket.Retain = isRetainedApplicationMessage;

            if (publishPacket.QualityOfServiceLevel > 0)
            {
                publishPacket.PacketIdentifier = _packetIdentifierProvider.GetNextPacketIdentifier();
            }

            if (_options.ClientMessageQueueInterceptor != null)
            {
                var context = new MqttClientMessageQueueInterceptorContext(
                    senderClientSession?.ClientId,
                    ClientId,
                    applicationMessage);

                if (_options.ClientMessageQueueInterceptor != null)
                {
                    await _options.ClientMessageQueueInterceptor.InterceptClientMessageQueueEnqueueAsync(context).ConfigureAwait(false);
                }

                if (!context.AcceptEnqueue || context.ApplicationMessage == null)
                {
                    return;
                }

                publishPacket.Topic = context.ApplicationMessage.Topic;
                publishPacket.Payload = context.ApplicationMessage.Payload;
                publishPacket.QualityOfServiceLevel = context.ApplicationMessage.QualityOfServiceLevel;
            }

            _pendingMessagesQueue.Enqueue(publishPacket);
        }



        private async Task RunInternalAsync(MqttConnectPacket connectPacket, IMqttChannelAdapter channelAdapter)
        {
            if (channelAdapter == null) throw new ArgumentNullException(nameof(channelAdapter));

            try
            {
                _logger.Info("Client '{0}': Connected.", ClientId);
                _eventDispatcher.OnClientConnected(ClientId);

                _channelAdapter = channelAdapter;

                _channelAdapter.ReadingPacketStarted += OnAdapterReadingPacketStarted;
                _channelAdapter.ReadingPacketCompleted += OnAdapterReadingPacketCompleted;

                var cancellationTokenSource = new CancellationTokenSource();
                _cancellationTokenSource = cancellationTokenSource;

                _wasCleanDisconnect = false;
                _willMessage = connectPacket.WillMessage;

                _pendingMessagesQueue.Start(channelAdapter, cancellationTokenSource.Token);
                _keepAliveMonitor.Start(connectPacket.KeepAlivePeriod, cancellationTokenSource.Token);

                await channelAdapter.SendPacketAsync(
                    new MqttConnAckPacket
                    {
                        ReturnCode = MqttConnectReturnCode.ConnectionAccepted,
                        ReasonCode = MqttConnectReasonCode.Success,
                        IsSessionPresent = _isCleanSession
                    },
                    cancellationTokenSource.Token).ConfigureAwait(false);

                _isCleanSession = false;

                Task.Run(async () =>
                {
                    while (!cancellationTokenSource.IsCancellationRequested)
                    {
                        var packet = _packetDispatcher.Take(cancellationTokenSource.Token);
                        await ProcessReceivedPacketAsync(packet, cancellationTokenSource.Token).ConfigureAwait(false);
                    }
                }, cancellationTokenSource.Token);

                Task.Run(async () =>
                {
                    while (!cancellationTokenSource.IsCancellationRequested)
                    {
                        try
                        {
                            var packet = await _outboundMessageStream.TakeAsync(cancellationTokenSource.Token);
                            await channelAdapter.SendPacketAsync(packet, cancellationTokenSource.Token);
                        }
                        catch (Exception e)
                        {
                            _logger.Error(e, "sdfsdf");
                            await StopAsync(MqttClientDisconnectType.NotClean);

                        }
                        
                    }
                },cancellationTokenSource.Token);

                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    var packet = await channelAdapter.ReceivePacketAsync(TimeSpan.Zero, cancellationTokenSource.Token).ConfigureAwait(false);
                    if (packet != null)
                    {
                        _keepAliveMonitor.PacketReceived(packet);
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
                    _logger.Error(exception, "Client '{0}': Unhandled exception while receiving client packets.", ClientId);
                }

                StopInternal(MqttClientDisconnectType.NotClean);
            }
            finally
            {
                if (_willMessage != null && !_wasCleanDisconnect)
                {
                    _sessionsManager.EnqueueApplicationMessage(this, _willMessage);
                }

                _willMessage = null;

                _channelAdapter.ReadingPacketStarted -= OnAdapterReadingPacketStarted;
                _channelAdapter.ReadingPacketCompleted -= OnAdapterReadingPacketCompleted;
                _channelAdapter = null;

                _logger.Info("Client '{0}': Session stopped.", ClientId);
                _eventDispatcher.OnClientDisconnected(ClientId, _wasCleanDisconnect);

                _workerTask = null;
            }
        }

        private void StopInternal(MqttClientDisconnectType type)
        {
            var cts = _cancellationTokenSource;
            if (cts == null || cts.IsCancellationRequested)
            {
                return;
            }

            _wasCleanDisconnect = type == MqttClientDisconnectType.Clean;
            _cancellationTokenSource?.Cancel(false);
            _packetDispatcher.Reset();
        }

        private readonly MqttMessageStream _outboundMessageStream = new MqttMessageStream();

        private Task ProcessReceivedPacketAsync(MqttBasePacket packet, CancellationToken cancellationToken)
        {
            if (packet is MqttPublishPacket publishPacket)
            {
                return HandleIncomingPublishPacketAsync(publishPacket, cancellationToken);
            }

            if (packet is MqttPingReqPacket)
            {
                //return channelAdapter.SendPacketAsync(new MqttPingRespPacket(), cancellationToken);
                _outboundMessageStream.Enqueue(new MqttPingRespPacket());
                return Task.FromResult(0);
            }

            if (packet is MqttSubscribePacket subscribePacket)
            {
                return HandleIncomingSubscribePacketAsync(subscribePacket, cancellationToken);
            }

            if (packet is MqttUnsubscribePacket unsubscribePacket)
            {
                return HandleIncomingUnsubscribePacketAsync(unsubscribePacket, cancellationToken);
            }

            if (packet is MqttDisconnectPacket)
            {
                StopInternal(MqttClientDisconnectType.Clean);
                return Task.FromResult(0);
            }

            //if (packet is MqttAuthPacket ||
            //    packet is MqttSubAckPacket ||
            //    packet is MqttUnsubAckPacket ||
            //    packet is MqttPubAckPacket ||
            //    packet is MqttPubCompPacket ||
            //    packet is MqttPubRecPacket ||
            //    packet is MqttPubRelPacket)
            //{
            //    _packetDispatcher.TryDispatch(packet);
            //    return Task.FromResult(0);
            //}

            _logger.Warning(null, "Client '{0}': Received invalid packet ({1}). Closing connection.", ClientId, packet);

            StopInternal(MqttClientDisconnectType.NotClean);
            return Task.FromResult(0);
        }

        private async Task EnqueueSubscribedRetainedMessagesAsync(ICollection<TopicFilter> topicFilters)
        {
            var retainedMessages = await _retainedMessagesManager.GetSubscribedMessagesAsync(topicFilters).ConfigureAwait(false);
            foreach (var applicationMessage in retainedMessages)
            {
                await EnqueueApplicationMessageAsync(null, applicationMessage, true).ConfigureAwait(false);
            }
        }

        private async Task HandleIncomingSubscribePacketAsync(MqttSubscribePacket subscribePacket, CancellationToken cancellationToken)
        {
            var subscribeResult = await _subscriptionsManager.SubscribeAsync(subscribePacket).ConfigureAwait(false);

            _outboundMessageStream.Enqueue(subscribeResult.ResponsePacket);
            //await adapter.SendPacketAsync(subscribeResult.ResponsePacket, cancellationToken).ConfigureAwait(false);

            // TODO: Add "WaitForDelivery".

            if (subscribeResult.CloseConnection)
            {
                StopInternal(MqttClientDisconnectType.NotClean);
                return;
            }

            await EnqueueSubscribedRetainedMessagesAsync(subscribePacket.TopicFilters).ConfigureAwait(false);
        }

        private Task HandleIncomingUnsubscribePacketAsync(MqttUnsubscribePacket unsubscribePacket, CancellationToken cancellationToken)
        {
            var unsubscribeResult = _subscriptionsManager.Unsubscribe(unsubscribePacket);

            _outboundMessageStream.Enqueue(unsubscribeResult);

            //return adapter.SendPacketAsync(unsubscribeResult, cancellationToken);
            return Task.FromResult(0);
        }

        private Task HandleIncomingPublishPacketAsync(MqttPublishPacket publishPacket, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _receivedMessagesCount);

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

        private Task HandleIncomingPublishPacketWithQoS0Async(MqttPublishPacket publishPacket)
        {
            _sessionsManager.EnqueueApplicationMessage(
                this,
                _channelAdapter.PacketFormatterAdapter.DataConverter.CreateApplicationMessage(publishPacket));

            return Task.FromResult(0);
        }

        private Task HandleIncomingPublishPacketWithQoS1Async(
            MqttPublishPacket publishPacket,
            CancellationToken cancellationToken)
        {
            _sessionsManager.EnqueueApplicationMessage(
                this,
                _channelAdapter.PacketFormatterAdapter.DataConverter.CreateApplicationMessage(publishPacket));

            var response = new MqttPubAckPacket
            {
                PacketIdentifier = publishPacket.PacketIdentifier,
                ReasonCode = MqttPubAckReasonCode.Success
            };

            _outboundMessageStream.Enqueue(response);

            //return adapter.SendPacketAsync(response, cancellationToken);
            return Task.FromResult(0);
        }

        private async Task HandleIncomingPublishPacketWithQoS2Async(
            MqttPublishPacket publishPacket,
            CancellationToken cancellationToken)
        {
            // QoS 2 is implement as method "B" (4.3.3 QoS 2: Exactly once delivery)
            _sessionsManager.EnqueueApplicationMessage(this, _channelAdapter.PacketFormatterAdapter.DataConverter.CreateApplicationMessage(publishPacket));

            using (var pubRelPacketAwaiter = _packetDispatcher.AddPacketAwaiter<MqttPubRelPacket>(publishPacket.PacketIdentifier))
            {
                var pubRecPacket = new MqttPubRecPacket
                {
                    PacketIdentifier = publishPacket.PacketIdentifier,
                    ReasonCode = MqttPubRecReasonCode.Success
                };

                //await adapter.SendPacketAsync(pubRecPacket, cancellationToken).ConfigureAwait(false);
                _outboundMessageStream.Enqueue(pubRecPacket);

                await pubRelPacketAwaiter.WaitOneAsync(_options.DefaultCommunicationTimeout).ConfigureAwait(false);
            }
        }

        private void OnAdapterReadingPacketCompleted(object sender, EventArgs e)
        {
            _keepAliveMonitor?.Resume();
        }

        private void OnAdapterReadingPacketStarted(object sender, EventArgs e)
        {
            _keepAliveMonitor?.Pause();
        }
    }
}
