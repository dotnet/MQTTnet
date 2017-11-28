using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Serializer;

namespace MQTTnet.Server
{
    public sealed class MqttClientSession
    {
        private readonly Stopwatch _lastPacketReceivedTracker = new Stopwatch();
        private readonly Stopwatch _lastNonKeepAlivePacketReceivedTracker = new Stopwatch();

        private readonly MqttClientSubscriptionsManager _subscriptionsManager;
        private readonly MqttClientSessionsManager _sessionsManager;
        private readonly MqttClientPendingMessagesQueue _pendingMessagesQueue;
        private readonly IMqttServerOptions _options;
        private readonly IMqttNetLogger _logger;

        private IMqttChannelAdapter _adapter;
        private CancellationTokenSource _cancellationTokenSource;
        private MqttApplicationMessage _willMessage;

        public MqttClientSession(
            string clientId,
            IMqttServerOptions options,
            MqttClientSessionsManager sessionsManager,
            IMqttNetLogger logger)
        {
            _sessionsManager = sessionsManager ?? throw new ArgumentNullException(nameof(sessionsManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            ClientId = clientId;

            _options = options;

            _subscriptionsManager = new MqttClientSubscriptionsManager(_options);
            _pendingMessagesQueue = new MqttClientPendingMessagesQueue(_options, this, _logger);
        }

        public string ClientId { get; }

        public MqttProtocolVersion? ProtocolVersion => _adapter?.PacketSerializer.ProtocolVersion;

        public TimeSpan LastPacketReceived => _lastPacketReceivedTracker.Elapsed;

        public TimeSpan LastNonKeepAlivePacketReceived => _lastNonKeepAlivePacketReceivedTracker.Elapsed;

        public bool IsConnected => _adapter != null;

        public async Task RunAsync(MqttApplicationMessage willMessage, IMqttChannelAdapter adapter)
        {
            if (adapter == null) throw new ArgumentNullException(nameof(adapter));

            try
            {
                _lastPacketReceivedTracker.Restart();
                _lastNonKeepAlivePacketReceivedTracker.Restart();

                var cancellationTokenSource = new CancellationTokenSource();

                _willMessage = willMessage;
                _adapter = adapter;
                _cancellationTokenSource = cancellationTokenSource;

                _pendingMessagesQueue.Start(adapter, cancellationTokenSource.Token);
                await ReceivePacketsAsync(adapter, cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (MqttCommunicationException exception)
            {
                _logger.Warning<MqttClientSession>(exception, "Client '{0}': Communication exception while processing client packets.", ClientId);
            }
            catch (Exception exception)
            {
                _logger.Error<MqttClientSession>(exception, "Client '{0}': Unhandled exception while processing client packets.", ClientId);
            }
        }

        public async Task StopAsync()
        {
            try
            {
                if (_cancellationTokenSource == null)
                {
                    return;
                }

                _cancellationTokenSource?.Cancel(false);
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                if (_adapter != null)
                {
                    await _adapter.DisconnectAsync(_options.DefaultCommunicationTimeout).ConfigureAwait(false);
                    _adapter = null;
                }

                _logger.Info<MqttClientSession>("Client '{0}': Session stopped.", ClientId);
            }
            finally
            {
                var willMessage = _willMessage;
                if (willMessage != null)
                {
                    _willMessage = null; //clear willmessage so it is send just once
                    await _sessionsManager.DispatchApplicationMessageAsync(this, willMessage).ConfigureAwait(false);
                }
            }
        }

        public void EnqueueApplicationMessage(MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            var result = _subscriptionsManager.CheckSubscriptions(applicationMessage);
            if (!result.IsSubscribed)
            {
                return;
            }

            var publishPacket = applicationMessage.ToPublishPacket();
            publishPacket.QualityOfServiceLevel = result.QualityOfServiceLevel;
            _pendingMessagesQueue.Enqueue(publishPacket);
        }

        private async Task ReceivePacketsAsync(IMqttChannelAdapter adapter, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var packet = await adapter.ReceivePacketAsync(TimeSpan.Zero, cancellationToken).ConfigureAwait(false);
                    await ProcessReceivedPacketAsync(adapter, packet, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (MqttCommunicationException exception)
            {
                _logger.Warning<MqttClientSession>(exception, "Client '{0}': Communication exception while processing client packets.", ClientId);
                await StopAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.Error<MqttClientSession>(exception, "Client '{0}': Unhandled exception while processing client packets.", ClientId);
                await StopAsync().ConfigureAwait(false);
            }
        }

        private Task ProcessReceivedPacketAsync(IMqttChannelAdapter adapter, MqttBasePacket packet, CancellationToken cancellationToken)
        {
            _lastPacketReceivedTracker.Restart();

            if (!(packet is MqttPingReqPacket))
            {
                _lastNonKeepAlivePacketReceivedTracker.Restart();
            }

            if (packet is MqttPublishPacket publishPacket)
            {
                return HandleIncomingPublishPacketAsync(adapter, publishPacket, cancellationToken);
            }

            if (packet is MqttPingReqPacket)
            {
                return adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, cancellationToken, new MqttPingRespPacket());
            }

            if (packet is MqttPubRelPacket pubRelPacket)
            {
                return HandleIncomingPubRelPacketAsync(adapter, pubRelPacket, cancellationToken);
            }

            if (packet is MqttPubRecPacket pubRecPacket)
            {
                return adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, cancellationToken, pubRecPacket.CreateResponse<MqttPubRelPacket>());
            }

            if (packet is MqttPubAckPacket || packet is MqttPubCompPacket)
            {
                // Discard message.
                return Task.FromResult(0);
            }

            if (packet is MqttSubscribePacket subscribePacket)
            {
                return HandleIncomingSubscribePacketAsync(adapter, subscribePacket, cancellationToken);
            }

            if (packet is MqttUnsubscribePacket unsubscribePacket)
            {
                return adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, cancellationToken, _subscriptionsManager.Unsubscribe(unsubscribePacket));
            }

            if (packet is MqttDisconnectPacket || packet is MqttConnectPacket)
            {
                return StopAsync();
            }

            _logger.Warning<MqttClientSession>("Client '{0}': Received not supported packet ({1}). Closing connection.", ClientId, packet);
            return StopAsync();
        }

        private async Task HandleIncomingSubscribePacketAsync(IMqttChannelAdapter adapter, MqttSubscribePacket subscribePacket, CancellationToken cancellationToken)
        {
            var subscribeResult = _subscriptionsManager.Subscribe(subscribePacket, ClientId);

            await adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, cancellationToken, subscribeResult.ResponsePacket).ConfigureAwait(false);
            await EnqueueSubscribedRetainedMessagesAsync(subscribePacket).ConfigureAwait(false);

            if (subscribeResult.CloseConnection)
            {
                await adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, cancellationToken, new MqttDisconnectPacket()).ConfigureAwait(false);
                await StopAsync().ConfigureAwait(false);
            }
        }

        private async Task EnqueueSubscribedRetainedMessagesAsync(MqttSubscribePacket subscribePacket)
        {
            var retainedMessages = await _sessionsManager.GetRetainedMessagesAsync(subscribePacket).ConfigureAwait(false);
            foreach (var publishPacket in retainedMessages)
            {
                EnqueueApplicationMessage(publishPacket);
            }
        }

        private Task HandleIncomingPublishPacketAsync(IMqttChannelAdapter adapter, MqttPublishPacket publishPacket, CancellationToken cancellationToken)
        {
            var applicationMessage = publishPacket.ToApplicationMessage();

            switch (applicationMessage.QualityOfServiceLevel)
            {
                case MqttQualityOfServiceLevel.AtMostOnce:
                    {
                        return _sessionsManager.DispatchApplicationMessageAsync(this, applicationMessage);
                    }
                case MqttQualityOfServiceLevel.AtLeastOnce:
                    {
                        return HandleIncomingPublishPacketWithQoS1(adapter, applicationMessage, publishPacket, cancellationToken);
                    }
                case MqttQualityOfServiceLevel.ExactlyOnce:
                    {
                        return HandleIncomingPublishPacketWithQoS2(adapter, applicationMessage, publishPacket, cancellationToken);
                    }
                default:
                    {
                        throw new MqttCommunicationException("Received a not supported QoS level.");
                    }
            }
        }

        private async Task HandleIncomingPublishPacketWithQoS1(IMqttChannelAdapter adapter, MqttApplicationMessage applicationMessage, MqttPublishPacket publishPacket, CancellationToken cancellationToken)
        {
            await _sessionsManager.DispatchApplicationMessageAsync(this, applicationMessage).ConfigureAwait(false);

            var response = new MqttPubAckPacket { PacketIdentifier = publishPacket.PacketIdentifier };
            await adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, cancellationToken, response).ConfigureAwait(false);
        }

        private async Task HandleIncomingPublishPacketWithQoS2(IMqttChannelAdapter adapter, MqttApplicationMessage applicationMessage, MqttPublishPacket publishPacket, CancellationToken cancellationToken)
        {
            // QoS 2 is implement as method "B" [4.3.3 QoS 2: Exactly once delivery]
            await _sessionsManager.DispatchApplicationMessageAsync(this, applicationMessage).ConfigureAwait(false);

            var response = new MqttPubRecPacket { PacketIdentifier = publishPacket.PacketIdentifier };
            await adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, cancellationToken, response).ConfigureAwait(false);
        }

        private Task HandleIncomingPubRelPacketAsync(IMqttChannelAdapter adapter, MqttPubRelPacket pubRelPacket, CancellationToken cancellationToken)
        {
            var response = new MqttPubCompPacket { PacketIdentifier = pubRelPacket.PacketIdentifier };
            return adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, cancellationToken, response);
        }
    }
}
