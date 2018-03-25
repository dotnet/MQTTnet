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
    public sealed class MqttClientSession : IDisposable
    {
        private readonly MqttPacketIdentifierProvider _packetIdentifierProvider = new MqttPacketIdentifierProvider();
        private readonly IMqttServerOptions _options;
        private readonly IMqttNetLogger _logger;
        private readonly MqttRetainedMessagesManager _retainedMessagesManager;

        private IMqttChannelAdapter _adapter;
        private CancellationTokenSource _cancellationTokenSource;
        private MqttApplicationMessage _willMessage;

        public MqttClientSession(
            string clientId,
            IMqttServerOptions options,
            MqttRetainedMessagesManager retainedMessagesManager,
            IMqttNetLogger logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _retainedMessagesManager = retainedMessagesManager ?? throw new ArgumentNullException(nameof(retainedMessagesManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            ClientId = clientId;

            KeepAliveMonitor = new MqttClientKeepAliveMonitor(clientId, StopDueToKeepAliveTimeoutAsync, _logger);
            SubscriptionsManager = new MqttClientSubscriptionsManager(_options, clientId);
            PendingMessagesQueue = new MqttClientPendingMessagesQueue(_options, this, _logger);
        }

        public Func<MqttClientSession, MqttApplicationMessage, Task> ApplicationMessageReceivedCallback { get; set; }

        public MqttClientSubscriptionsManager SubscriptionsManager { get; }

        public MqttClientPendingMessagesQueue PendingMessagesQueue { get; }

        public MqttClientKeepAliveMonitor KeepAliveMonitor { get; }

        public string ClientId { get; }

        public MqttProtocolVersion? ProtocolVersion => _adapter?.PacketSerializer.ProtocolVersion;

        public bool IsConnected => _adapter != null;

        public async Task RunAsync(MqttConnectPacket connectPacket, IMqttChannelAdapter adapter)
        {
            if (connectPacket == null) throw new ArgumentNullException(nameof(connectPacket));
            if (adapter == null) throw new ArgumentNullException(nameof(adapter));

            try
            {
                var cancellationTokenSource = new CancellationTokenSource();

                _willMessage = connectPacket.WillMessage;
                _adapter = adapter;
                _cancellationTokenSource = cancellationTokenSource;

                PendingMessagesQueue.Start(adapter, cancellationTokenSource.Token);
                KeepAliveMonitor.Start(connectPacket.KeepAlivePeriod, cancellationTokenSource.Token);

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
                    _willMessage = null; // clear willmessage so it is send just once
                    await ApplicationMessageReceivedCallback(this, willMessage).ConfigureAwait(false);
                }
            }
        }

        public async Task EnqueueApplicationMessageAsync(MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            var result = await SubscriptionsManager.CheckSubscriptionsAsync(applicationMessage);
            if (!result.IsSubscribed)
            {
                return;
            }

            var publishPacket = applicationMessage.ToPublishPacket();
            publishPacket.QualityOfServiceLevel = result.QualityOfServiceLevel;

            if (publishPacket.QualityOfServiceLevel > 0)
            {
                publishPacket.PacketIdentifier = _packetIdentifierProvider.GetNewPacketIdentifier();
            }

            PendingMessagesQueue.Enqueue(publishPacket);
        }

        public async Task SubscribeAsync(IList<TopicFilter> topicFilters)
        {
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            await SubscriptionsManager.SubscribeAsync(new MqttSubscribePacket
            {
                TopicFilters = topicFilters
            }).ConfigureAwait(false);

            await EnqueueSubscribedRetainedMessagesAsync(topicFilters).ConfigureAwait(false);
        }

        public Task UnsubscribeAsync(IList<string> topicFilters)
        {
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            return SubscriptionsManager.UnsubscribeAsync(new MqttUnsubscribePacket
            {
                TopicFilters = topicFilters
            });
        }

        public void Dispose()
        {
            ApplicationMessageReceivedCallback = null;

            SubscriptionsManager?.Dispose();
            PendingMessagesQueue?.Dispose();

            _cancellationTokenSource?.Dispose();
        }

        private Task StopDueToKeepAliveTimeoutAsync()
        {
            _logger.Info<MqttClientSession>("Client '{0}': Timeout while waiting for KeepAlive packet.", ClientId);
            return StopAsync();
        }

        private async Task ReceivePacketsAsync(IMqttChannelAdapter adapter, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var packet = await adapter.ReceivePacketAsync(TimeSpan.Zero, cancellationToken).ConfigureAwait(false);
                    KeepAliveMonitor.PacketReceived(packet);
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
            if (packet is MqttPublishPacket publishPacket)
            {
                return HandleIncomingPublishPacketAsync(adapter, publishPacket, cancellationToken);
            }

            if (packet is MqttPingReqPacket)
            {
                return adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, cancellationToken, new[] { new MqttPingRespPacket() });
            }

            if (packet is MqttPubRelPacket pubRelPacket)
            {
                return HandleIncomingPubRelPacketAsync(adapter, pubRelPacket, cancellationToken);
            }

            if (packet is MqttPubRecPacket pubRecPacket)
            {
                var responsePacket = new MqttPubRelPacket
                {
                    PacketIdentifier = pubRecPacket.PacketIdentifier
                };

                return adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, cancellationToken, new[] { responsePacket });
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
                return HandleIncomingUnsubscribePacketAsync(adapter, unsubscribePacket, cancellationToken);
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
            var subscribeResult = await SubscriptionsManager.SubscribeAsync(subscribePacket).ConfigureAwait(false);
            await adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, cancellationToken, new[] { subscribeResult.ResponsePacket }).ConfigureAwait(false);

            if (subscribeResult.CloseConnection)
            {
                await adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, cancellationToken, new[] { new MqttDisconnectPacket() }).ConfigureAwait(false);
                await StopAsync().ConfigureAwait(false);
            }

            await EnqueueSubscribedRetainedMessagesAsync(subscribePacket.TopicFilters).ConfigureAwait(false);
        }

        private async Task HandleIncomingUnsubscribePacketAsync(IMqttChannelAdapter adapter, MqttUnsubscribePacket unsubscribePacket, CancellationToken cancellationToken)
        {
            var unsubscribeResult = await SubscriptionsManager.UnsubscribeAsync(unsubscribePacket).ConfigureAwait(false);
            await adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, cancellationToken, new[] { unsubscribeResult });
        }

        private async Task EnqueueSubscribedRetainedMessagesAsync(ICollection<TopicFilter> topicFilters)
        {
            var retainedMessages = await _retainedMessagesManager.GetSubscribedMessagesAsync(topicFilters);
            foreach (var applicationMessage in retainedMessages)
            {
                await EnqueueApplicationMessageAsync(applicationMessage).ConfigureAwait(false);
            }
        }

        private Task HandleIncomingPublishPacketAsync(IMqttChannelAdapter adapter, MqttPublishPacket publishPacket, CancellationToken cancellationToken)
        {
            var applicationMessage = publishPacket.ToApplicationMessage();

            switch (applicationMessage.QualityOfServiceLevel)
            {
                case MqttQualityOfServiceLevel.AtMostOnce:
                    {
                        return ApplicationMessageReceivedCallback?.Invoke(this, applicationMessage);
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
            await ApplicationMessageReceivedCallback(this, applicationMessage).ConfigureAwait(false);

            var response = new MqttPubAckPacket { PacketIdentifier = publishPacket.PacketIdentifier };
            await adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, cancellationToken, new[] { response }).ConfigureAwait(false);
        }

        private async Task HandleIncomingPublishPacketWithQoS2(IMqttChannelAdapter adapter, MqttApplicationMessage applicationMessage, MqttPublishPacket publishPacket, CancellationToken cancellationToken)
        {
            // QoS 2 is implement as method "B" [4.3.3 QoS 2: Exactly once delivery]
            await ApplicationMessageReceivedCallback(this, applicationMessage).ConfigureAwait(false);

            var response = new MqttPubRecPacket { PacketIdentifier = publishPacket.PacketIdentifier };
            await adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, cancellationToken, new[] { response }).ConfigureAwait(false);
        }

        private Task HandleIncomingPubRelPacketAsync(IMqttChannelAdapter adapter, MqttPubRelPacket pubRelPacket, CancellationToken cancellationToken)
        {
            var response = new MqttPubCompPacket { PacketIdentifier = pubRelPacket.PacketIdentifier };
            return adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, cancellationToken, new[] { response });
        }
    }
}
