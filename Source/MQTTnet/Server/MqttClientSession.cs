using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Serializer;

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
        private IDisposable _cleanupHandle;

        private string _adapterEndpoint;
        private MqttProtocolVersion? _adapterProtocolVersion;
        
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

        public Task RunAsync(MqttConnectPacket connectPacket, IMqttChannelAdapter adapter)
        {
            _workerTask = RunInternalAsync(connectPacket, adapter);
            return _workerTask;
        }

        public void Stop(MqttClientDisconnectType type)
        {
            Stop(type, false);
        }

        public void EnqueueApplicationMessage(MqttClientSession senderClientSession, MqttPublishPacket publishPacket)
        {
            if (publishPacket == null) throw new ArgumentNullException(nameof(publishPacket));

            var checkSubscriptionsResult = _subscriptionsManager.CheckSubscriptions(publishPacket.Topic, publishPacket.QualityOfServiceLevel);
            if (!checkSubscriptionsResult.IsSubscribed)
            {
                return;
            }

            publishPacket = new MqttPublishPacket
            {
                Topic = publishPacket.Topic,
                Payload = publishPacket.Payload,
                QualityOfServiceLevel = checkSubscriptionsResult.QualityOfServiceLevel,
                Retain = publishPacket.Retain,
                Dup = false
            };

            if (publishPacket.QualityOfServiceLevel > 0)
            {
                publishPacket.PacketIdentifier = _packetIdentifierProvider.GetNewPacketIdentifier();
            }

            if (_options.ClientMessageQueueInterceptor != null)
            {
                var context = new MqttClientMessageQueueInterceptorContext(
                    senderClientSession?.ClientId,
                    ClientId,
                    publishPacket.ToApplicationMessage());

                _options.ClientMessageQueueInterceptor?.Invoke(context);

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

        public Task SubscribeAsync(IList<TopicFilter> topicFilters)
        {
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            _subscriptionsManager.Subscribe(new MqttSubscribePacket
            {
                TopicFilters = topicFilters
            });

            EnqueueSubscribedRetainedMessages(topicFilters);
            return Task.FromResult(0);
        }

        public Task UnsubscribeAsync(IList<string> topicFilters)
        {
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            _subscriptionsManager.Unsubscribe(new MqttUnsubscribePacket
            {
                TopicFilters = topicFilters
            });

            return Task.FromResult(0);
        }

        public void ClearPendingApplicationMessages()
        {
            _pendingPacketsQueue.Clear();
        }

        public void Dispose()
        {
            _pendingPacketsQueue?.Dispose();

            _cancellationTokenSource?.Cancel ();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        private void Stop(MqttClientDisconnectType type, bool isInsideSession)
        {
            try
            {
                var cts = _cancellationTokenSource;
                if (cts == null || cts.IsCancellationRequested)
                {
                    return;
                }

                _cancellationTokenSource?.Cancel(false);

                _wasCleanDisconnect = type == MqttClientDisconnectType.Clean;

                if (_willMessage != null && !_wasCleanDisconnect)
                {
                    _sessionsManager.EnqueueApplicationMessage(this, _willMessage.ToPublishPacket());
                }

                _willMessage = null;

                if (!isInsideSession)
                {
                    _workerTask?.GetAwaiter().GetResult();
                }
            }
            finally
            {
                _logger.Info("Client '{0}': Disconnected (clean={1}).", ClientId, _wasCleanDisconnect);
                _eventDispatcher.OnClientDisconnected(ClientId, _wasCleanDisconnect);
            }
        }

        private async Task RunInternalAsync(MqttConnectPacket connectPacket, IMqttChannelAdapter adapter)
        {
            if (connectPacket == null) throw new ArgumentNullException(nameof(connectPacket));
            if (adapter == null) throw new ArgumentNullException(nameof(adapter));

            try
            {
                if (_cancellationTokenSource != null)
                {
                    Stop(MqttClientDisconnectType.Clean, true);
                }

                adapter.ReadingPacketStarted += OnAdapterReadingPacketStarted;
                adapter.ReadingPacketCompleted += OnAdapterReadingPacketCompleted;

                _cancellationTokenSource = new CancellationTokenSource();

                //workaround for https://github.com/dotnet/corefx/issues/24430
#pragma warning disable 4014
                _cleanupHandle = _cancellationTokenSource.Token.Register(async () =>
                {
                    await TryDisconnectAdapterAsync(adapter).ConfigureAwait(false);
                    TryDisposeAdapter(adapter);
                });
#pragma warning restore 4014
                //end workaround

                _wasCleanDisconnect = false;
                _willMessage = connectPacket.WillMessage;

                _pendingPacketsQueue.Start(adapter, _cancellationTokenSource.Token);
                _keepAliveMonitor.Start(connectPacket.KeepAlivePeriod, _cancellationTokenSource.Token);

                _adapterEndpoint = adapter.Endpoint;
                _adapterProtocolVersion = adapter.PacketSerializer.ProtocolVersion;

                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    var packet = await adapter.ReceivePacketAsync(TimeSpan.Zero, _cancellationTokenSource.Token).ConfigureAwait(false);
                    if (packet != null)
                    {
                        _keepAliveMonitor.PacketReceived(packet);
                        ProcessReceivedPacket(adapter, packet, _cancellationTokenSource.Token);
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

                Stop(MqttClientDisconnectType.NotClean, true);
            }
            finally
            {
                _adapterEndpoint = null;
                _adapterProtocolVersion = null;

                // Uncomment as soon as the workaround above is no longer needed.
                //await TryDisconnectAdapterAsync(adapter).ConfigureAwait(false);
                //TryDisposeAdapter(adapter);

                _cleanupHandle?.Dispose();
                _cleanupHandle = null;
                
                _cancellationTokenSource?.Cancel(false);
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private void TryDisposeAdapter(IMqttChannelAdapter adapter)
        {
            if (adapter == null)
            {
                return;
            }

            try
            {
                adapter.ReadingPacketStarted -= OnAdapterReadingPacketStarted;
                adapter.ReadingPacketCompleted -= OnAdapterReadingPacketCompleted;

                adapter.Dispose();
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Error while disposing channel adapter.");
            }
        }

        private async Task TryDisconnectAdapterAsync(IMqttChannelAdapter adapter)
        {
            if (adapter == null)
            {
                return;
            }

            try
            {
                await adapter.DisconnectAsync(_options.DefaultCommunicationTimeout, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Error while disconnecting channel adapter.");
            }
        }

        private void ProcessReceivedPacket(IMqttChannelAdapter adapter, MqttBasePacket packet, CancellationToken cancellationToken)
        {
            if (packet is MqttPublishPacket publishPacket)
            {
                HandleIncomingPublishPacket(adapter, publishPacket, cancellationToken);
                return;
            }

            if (packet is MqttPingReqPacket)
            {
                adapter.SendPacketAsync(new MqttPingRespPacket(), cancellationToken).GetAwaiter().GetResult();
                return;
            }

            if (packet is MqttPubRelPacket pubRelPacket)
            {
                var responsePacket = new MqttPubCompPacket
                {
                    PacketIdentifier = pubRelPacket.PacketIdentifier
                };

                adapter.SendPacketAsync(responsePacket, cancellationToken).GetAwaiter().GetResult();
                return;
            }

            if (packet is MqttPubRecPacket pubRecPacket)
            {
                var responsePacket = new MqttPubRelPacket
                {
                    PacketIdentifier = pubRecPacket.PacketIdentifier
                };

                adapter.SendPacketAsync(responsePacket, cancellationToken).GetAwaiter().GetResult();
                return;
            }

            if (packet is MqttPubAckPacket || packet is MqttPubCompPacket)
            {
                return;
            }

            if (packet is MqttSubscribePacket subscribePacket)
            {
                HandleIncomingSubscribePacket(adapter, subscribePacket, cancellationToken);
                return;
            }

            if (packet is MqttUnsubscribePacket unsubscribePacket)
            {
                HandleIncomingUnsubscribePacket(adapter, unsubscribePacket, cancellationToken);
                return;
            }

            if (packet is MqttDisconnectPacket)
            {
                Stop(MqttClientDisconnectType.Clean, true);
                return;
            }

            if (packet is MqttConnectPacket)
            {
                Stop(MqttClientDisconnectType.NotClean, true);
                return;
            }

            _logger.Warning(null, "Client '{0}': Received not supported packet ({1}). Closing connection.", ClientId, packet);
            Stop(MqttClientDisconnectType.NotClean, true);
        }

        private void EnqueueSubscribedRetainedMessages(ICollection<TopicFilter> topicFilters)
        {
            var retainedMessages = _retainedMessagesManager.GetSubscribedMessages(topicFilters);
            foreach (var applicationMessage in retainedMessages)
            {
                var publishPacket = applicationMessage.ToPublishPacket();

                // Set the retain flag to true according to [MQTT-3.3.1-8].
                publishPacket.Retain = true;

                EnqueueApplicationMessage(null, publishPacket);
            }
        }

        private void HandleIncomingSubscribePacket(IMqttChannelAdapter adapter, MqttSubscribePacket subscribePacket, CancellationToken cancellationToken)
        {
            var subscribeResult = _subscriptionsManager.Subscribe(subscribePacket);
            adapter.SendPacketAsync(subscribeResult.ResponsePacket, cancellationToken).GetAwaiter().GetResult();

            if (subscribeResult.CloseConnection)
            {
                Stop(MqttClientDisconnectType.NotClean, true);
                return;
            }

            EnqueueSubscribedRetainedMessages(subscribePacket.TopicFilters);
        }

        private void HandleIncomingUnsubscribePacket(IMqttChannelAdapter adapter, MqttUnsubscribePacket unsubscribePacket, CancellationToken cancellationToken)
        {
            var unsubscribeResult = _subscriptionsManager.Unsubscribe(unsubscribePacket);
            adapter.SendPacketAsync(unsubscribeResult, cancellationToken).GetAwaiter().GetResult();
        }

        private void HandleIncomingPublishPacket(IMqttChannelAdapter adapter, MqttPublishPacket publishPacket, CancellationToken cancellationToken)
        {
            switch (publishPacket.QualityOfServiceLevel)
            {
                case MqttQualityOfServiceLevel.AtMostOnce:
                    {
                        HandleIncomingPublishPacketWithQoS0(publishPacket);
                        break;
                    }
                case MqttQualityOfServiceLevel.AtLeastOnce:
                    {
                        HandleIncomingPublishPacketWithQoS1(adapter, publishPacket, cancellationToken);
                        break;
                    }
                case MqttQualityOfServiceLevel.ExactlyOnce:
                    {
                        HandleIncomingPublishPacketWithQoS2(adapter, publishPacket, cancellationToken);
                        break;
                    }
                default:
                    {
                        throw new MqttCommunicationException("Received a not supported QoS level.");
                    }
            }
        }

        private void HandleIncomingPublishPacketWithQoS0(MqttPublishPacket publishPacket)
        {
            _sessionsManager.EnqueueApplicationMessage(this, publishPacket);
        }

        private void HandleIncomingPublishPacketWithQoS1(
            IMqttChannelAdapter adapter,
            MqttPublishPacket publishPacket,
            CancellationToken cancellationToken)
        {
            _sessionsManager.EnqueueApplicationMessage(this, publishPacket);

            var response = new MqttPubAckPacket
            {
                PacketIdentifier = publishPacket.PacketIdentifier
            };

            adapter.SendPacketAsync(response, cancellationToken).GetAwaiter().GetResult();
        }

        private void HandleIncomingPublishPacketWithQoS2(
            IMqttChannelAdapter adapter,
            MqttPublishPacket publishPacket,
            CancellationToken cancellationToken)
        {
            // QoS 2 is implement as method "B" (4.3.3 QoS 2: Exactly once delivery)
            _sessionsManager.EnqueueApplicationMessage(this, publishPacket);

            var response = new MqttPubRecPacket
            {
                PacketIdentifier = publishPacket.PacketIdentifier
            };

            adapter.SendPacketAsync(response, cancellationToken).GetAwaiter().GetResult();
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
