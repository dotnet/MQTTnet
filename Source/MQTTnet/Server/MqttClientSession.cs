using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public class MqttClientSession : IMqttClientSession
    {
        private readonly MqttPacketIdentifierProvider _packetIdentifierProvider = new MqttPacketIdentifierProvider();

        private readonly MqttRetainedMessagesManager _retainedMessagesManager;
        private readonly MqttServerEventDispatcher _eventDispatcher;
        private readonly MqttClientKeepAliveMonitor _keepAliveMonitor;
        private readonly MqttClientPendingPacketsQueue _pendingPacketsQueue;
        private readonly MqttClientSubscriptionsManager _subscriptionsManager;
        private readonly MqttClientSessionsManager _sessionsManager;

        private readonly IMqttNetChildLogger _logger;
        private readonly IMqttServerOptions _options;

        private CancellationTokenSource _cancellationTokenSource;
        private MqttApplicationMessage _willMessage;
        private bool _wasCleanDisconnect;
        private Task _workerTask;
        //private IDisposable _cleanupHandle;

        private string _adapterEndpoint;
        private MqttProtocolVersion? _adapterProtocolVersion;
        private IMqttChannelAdapter _channelAdapter;

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
            _pendingPacketsQueue = new MqttClientPendingPacketsQueue(_options, this, _logger);
        }

        public string ClientId { get; }

        public void FillStatus(MqttClientSessionStatus status)
        {
            status.ClientId = ClientId;
            status.IsConnected = _cancellationTokenSource != null;
            status.Endpoint = _adapterEndpoint;
            status.ProtocolVersion = _adapterProtocolVersion;
            status.PendingApplicationMessagesCount = _pendingPacketsQueue.Count;
            status.LastPacketReceived = _keepAliveMonitor.LastPacketReceived;
            status.LastNonKeepAlivePacketReceived = _keepAliveMonitor.LastNonKeepAlivePacketReceived;
        }

        //public Task RunAsync(MqttConnectPacket connectPacket, IMqttChannelAdapter adapter)
        //{
        //    _workerTask = RunInternalAsync(connectPacket, adapter);
        //    return _workerTask;
        //}

        public Task StopAsync(MqttClientDisconnectType type)
        {
            return StopAsync(type, false);
        }

        public async Task SubscribeAsync(IList<TopicFilter> topicFilters)
        {
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            var packet = new MqttSubscribePacket();
            packet.TopicFilters.AddRange(topicFilters);

            await _subscriptionsManager.SubscribeAsync(packet).ConfigureAwait(false);
            await EnqueueSubscribedRetainedMessagesAsync(topicFilters).ConfigureAwait(false);
        }

        public Task UnsubscribeAsync(IList<string> topicFilters)
        {
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            var packet = new MqttUnsubscribePacket();
            packet.TopicFilters.AddRange(topicFilters);

            _subscriptionsManager.Unsubscribe(packet);

            return Task.FromResult(0);
        }

        public void ClearPendingApplicationMessages()
        {
            _pendingPacketsQueue.Clear();
        }

        public void Dispose()
        {
            _pendingPacketsQueue?.Dispose();

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        public Task RunAsync(MqttConnectPacket connectPacket, IMqttChannelAdapter adapter)
        {
            _workerTask = RunInternalAsync(connectPacket, adapter);
            return _workerTask;
        }

        private async Task RunInternalAsync(MqttConnectPacket connectPacket, IMqttChannelAdapter adapter)
        {
            if (adapter == null) throw new ArgumentNullException(nameof(adapter));

            try
            {
                //if (_cancellationTokenSource != null)
                //{
                //    await StopAsync(MqttClientDisconnectType.Clean, true).ConfigureAwait(false);
                //}

                adapter.ReadingPacketStarted += OnAdapterReadingPacketStarted;
                adapter.ReadingPacketCompleted += OnAdapterReadingPacketCompleted;

                _cancellationTokenSource = new CancellationTokenSource();

//                //workaround for https://github.com/dotnet/corefx/issues/24430
//#pragma warning disable 4014
//                _cleanupHandle = _cancellationTokenSource.Token.Register(async () =>
//                {
//                    await TryDisconnectAdapterAsync(adapter).ConfigureAwait(false);
//                    TryDisposeAdapter(adapter);
//                });
//#pragma warning restore 4014
//                //end workaround

                _wasCleanDisconnect = false;
                _willMessage = connectPacket.WillMessage;

                _pendingPacketsQueue.Start(adapter, _cancellationTokenSource.Token);
                _keepAliveMonitor.Start(connectPacket.KeepAlivePeriod, _cancellationTokenSource.Token);

                _adapterEndpoint = adapter.Endpoint;
                _adapterProtocolVersion = adapter.PacketFormatterAdapter.ProtocolVersion;
                _channelAdapter = adapter;

                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    var packet = await adapter.ReceivePacketAsync(TimeSpan.Zero, _cancellationTokenSource.Token).ConfigureAwait(false);
                    if (packet != null)
                    {
                        _keepAliveMonitor.PacketReceived(packet);
                        await ProcessReceivedPacketAsync(adapter, packet, _cancellationTokenSource.Token).ConfigureAwait(false);
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
                    if (exception is MqttCommunicationClosedGracefullyException)
                    {
                        _logger.Verbose("Client '{0}': Connection closed gracefully.", ClientId);
                    }
                    else
                    {
                        _logger.Warning(exception, "Client '{0}': Communication exception while receiving client packets.", ClientId);
                    }
                }
                else
                {
                    _logger.Error(exception, "Client '{0}': Unhandled exception while receiving client packets.", ClientId);
                }

                await StopAsync(MqttClientDisconnectType.NotClean, true).ConfigureAwait(false);
            }
            finally
            {
                adapter.ReadingPacketStarted -= OnAdapterReadingPacketStarted;
                adapter.ReadingPacketCompleted -= OnAdapterReadingPacketCompleted;

                _adapterEndpoint = null;
                _adapterProtocolVersion = null;

                // Uncomment as soon as the workaround above is no longer needed.
                // Also called in outer scope!
                //await TryDisconnectAdapterAsync(adapter).ConfigureAwait(false);
                //TryDisposeAdapter(adapter);

                //_cleanupHandle?.Dispose();
                //_cleanupHandle = null;

                _cancellationTokenSource?.Cancel(false);
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                _workerTask = null;
            }
        }

        ////private void TryDisposeAdapter(IMqttChannelAdapter adapter)
        ////{
        ////    if (adapter == null)
        ////    {
        ////        return;
        ////    }

        ////    try
        ////    {
        ////        adapter.ReadingPacketStarted -= OnAdapterReadingPacketStarted;
        ////        adapter.ReadingPacketCompleted -= OnAdapterReadingPacketCompleted;

        ////        adapter.Dispose();
        ////    }
        ////    catch (Exception exception)
        ////    {
        ////        _logger.Error(exception, exception.Message);
        ////    }
        ////    finally
        ////    {
        ////        adapter.Dispose();
        ////    }
        ////}

        private async Task StopAsync(MqttClientDisconnectType type, bool isInsideSession)
        {
            try
            {
                var cts = _cancellationTokenSource;
                if (cts == null || cts.IsCancellationRequested)
                {
                    return;
                }

                _wasCleanDisconnect = type == MqttClientDisconnectType.Clean;

                _cancellationTokenSource?.Cancel(false);

                if (_willMessage != null && !_wasCleanDisconnect)
                {
                    _sessionsManager.EnqueueApplicationMessage(this, _willMessage);
                }

                _willMessage = null;

                if (!isInsideSession)
                {
                    if (_workerTask != null)
                    {
                        await _workerTask.ConfigureAwait(false);
                    }
                }
                
                await Task.FromResult(0);
            }
            finally
            {
                _logger.Info("Client '{0}': Session stopped.", ClientId);
                _eventDispatcher.OnClientDisconnected(ClientId, _wasCleanDisconnect);
            }
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

            // Set the retain flag to true according to [MQTT-3.3.1-8] and [MQTT-3.3.1-9].
            publishPacket.Retain = isRetainedApplicationMessage;

            if (publishPacket.QualityOfServiceLevel > 0)
            {
                publishPacket.PacketIdentifier = _packetIdentifierProvider.GetNewPacketIdentifier();
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

            _pendingPacketsQueue.Enqueue(publishPacket);
        }

        //private async Task TryDisconnectAdapterAsync(IMqttChannelAdapter adapter)
        //{
        //    if (adapter == null)
        //    {
        //        return;
        //    }

        //    try
        //    {
        //        await adapter.DisconnectAsync(_options.DefaultCommunicationTimeout, CancellationToken.None).ConfigureAwait(false);
        //    }
        //    catch (Exception exception)
        //    {
        //        _logger.Error(exception, "Error while disconnecting channel adapter.");
        //    }
        //}

        private Task ProcessReceivedPacketAsync(IMqttChannelAdapter adapter, MqttBasePacket packet, CancellationToken cancellationToken)
        {
            if (packet is MqttPublishPacket publishPacket)
            {
                return HandleIncomingPublishPacketAsync(adapter, publishPacket, cancellationToken);
            }

            if (packet is MqttPingReqPacket)
            {
                return adapter.SendPacketAsync(new MqttPingRespPacket(), cancellationToken);
            }

            if (packet is MqttPubRelPacket pubRelPacket)
            {
                var responsePacket = new MqttPubCompPacket
                {
                    PacketIdentifier = pubRelPacket.PacketIdentifier,
                    ReasonCode = MqttPubCompReasonCode.Success
                };

                return adapter.SendPacketAsync(responsePacket, cancellationToken);
            }

            if (packet is MqttPubRecPacket pubRecPacket)
            {
                var responsePacket = new MqttPubRelPacket
                {
                    PacketIdentifier = pubRecPacket.PacketIdentifier,
                    ReasonCode = MqttPubRelReasonCode.Success
                };

                return adapter.SendPacketAsync(responsePacket, cancellationToken);
            }

            if (packet is MqttPubAckPacket || packet is MqttPubCompPacket)
            {
                return Task.FromResult(0);
            }

            if (packet is MqttSubscribePacket subscribePacket)
            {
                return HandleIncomingSubscribePacketAsync(adapter, subscribePacket, cancellationToken);
            }

            if (packet is MqttUnsubscribePacket unsubscribePacket)
            {
                return HandleIncomingUnsubscribePacketAsync(adapter, unsubscribePacket, cancellationToken);
            }

            if (packet is MqttDisconnectPacket)
            {
                return StopAsync(MqttClientDisconnectType.Clean, true);
            }

            if (packet is MqttConnectPacket)
            {
                return StopAsync(MqttClientDisconnectType.NotClean, true);
            }

            _logger.Warning(null, "Client '{0}': Received not supported packet ({1}). Closing connection.", ClientId, packet);

            return StopAsync(MqttClientDisconnectType.NotClean, true);
        }

        private async Task EnqueueSubscribedRetainedMessagesAsync(ICollection<TopicFilter> topicFilters)
        {
            var retainedMessages = await _retainedMessagesManager.GetSubscribedMessagesAsync(topicFilters).ConfigureAwait(false);
            foreach (var applicationMessage in retainedMessages)
            {
                await EnqueueApplicationMessageAsync(null, applicationMessage, true).ConfigureAwait(false);
            }
        }

        private async Task HandleIncomingSubscribePacketAsync(IMqttChannelAdapter adapter, MqttSubscribePacket subscribePacket, CancellationToken cancellationToken)
        {
            var subscribeResult = await _subscriptionsManager.SubscribeAsync(subscribePacket).ConfigureAwait(false);
            await adapter.SendPacketAsync(subscribeResult.ResponsePacket, cancellationToken).ConfigureAwait(false);

            if (subscribeResult.CloseConnection)
            {
                await StopAsync(MqttClientDisconnectType.NotClean, true).ConfigureAwait(false);
            }

            await EnqueueSubscribedRetainedMessagesAsync(subscribePacket.TopicFilters).ConfigureAwait(false);
        }

        private Task HandleIncomingUnsubscribePacketAsync(IMqttChannelAdapter adapter, MqttUnsubscribePacket unsubscribePacket, CancellationToken cancellationToken)
        {
            var unsubscribeResult = _subscriptionsManager.Unsubscribe(unsubscribePacket);
            return adapter.SendPacketAsync(unsubscribeResult, cancellationToken);
        }

        private Task HandleIncomingPublishPacketAsync(IMqttChannelAdapter adapter, MqttPublishPacket publishPacket, CancellationToken cancellationToken)
        {
            switch (publishPacket.QualityOfServiceLevel)
            {
                case MqttQualityOfServiceLevel.AtMostOnce:
                    {
                        HandleIncomingPublishPacketWithQoS0(publishPacket);
                        return Task.FromResult(0);
                    }
                case MqttQualityOfServiceLevel.AtLeastOnce:
                    {
                        return HandleIncomingPublishPacketWithQoS1Async(adapter, publishPacket, cancellationToken);
                    }
                case MqttQualityOfServiceLevel.ExactlyOnce:
                    {
                        return HandleIncomingPublishPacketWithQoS2Async(adapter, publishPacket, cancellationToken);
                    }
                default:
                    {
                        throw new MqttCommunicationException("Received a not supported QoS level.");
                    }
            }
        }

        private void HandleIncomingPublishPacketWithQoS0(MqttPublishPacket publishPacket)
        {
            _sessionsManager.EnqueueApplicationMessage(
                this,
                _channelAdapter.PacketFormatterAdapter.DataConverter.CreateApplicationMessage(publishPacket));
        }

        private Task HandleIncomingPublishPacketWithQoS1Async(
            IMqttChannelAdapter adapter,
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

            return adapter.SendPacketAsync(response, cancellationToken);
        }

        private Task HandleIncomingPublishPacketWithQoS2Async(
            IMqttChannelAdapter adapter,
            MqttPublishPacket publishPacket,
            CancellationToken cancellationToken)
        {
            // QoS 2 is implement as method "B" (4.3.3 QoS 2: Exactly once delivery)
            _sessionsManager.EnqueueApplicationMessage(this, _channelAdapter.PacketFormatterAdapter.DataConverter.CreateApplicationMessage(publishPacket));

            var response = new MqttPubRecPacket
            {
                PacketIdentifier = publishPacket.PacketIdentifier,
                ReasonCode = MqttPubRecReasonCode.Success
            };

            return adapter.SendPacketAsync(response, cancellationToken);
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
