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
        private readonly MqttRetainedMessagesManager _retainedMessagesManager;
        private readonly IMqttNetChildLogger _logger;
        private readonly IMqttServerOptions _options;
        private readonly MqttClientSessionsManager _sessionsManager;

        private CancellationTokenSource _cancellationTokenSource;
        private MqttApplicationMessage _willMessage;
        private bool _wasCleanDisconnect;

        public MqttClientSession(
            string clientId,
            IMqttServerOptions options,
            MqttClientSessionsManager sessionsManager,
            MqttRetainedMessagesManager retainedMessagesManager,
            IMqttNetChildLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            _options = options ?? throw new ArgumentNullException(nameof(options));
            _sessionsManager = sessionsManager;
            _retainedMessagesManager = retainedMessagesManager ?? throw new ArgumentNullException(nameof(retainedMessagesManager));
            
            ClientId = clientId;

            _logger = logger.CreateChildLogger(nameof(MqttClientSession));

            KeepAliveMonitor = new MqttClientKeepAliveMonitor(clientId, StopDueToKeepAliveTimeout, _logger);
            SubscriptionsManager = new MqttClientSubscriptionsManager(clientId, _options, sessionsManager.Server);
            PendingMessagesQueue = new MqttClientPendingMessagesQueue(_options, this, _logger);
        }

        public MqttClientSubscriptionsManager SubscriptionsManager { get; }

        public MqttClientPendingMessagesQueue PendingMessagesQueue { get; }

        public MqttClientKeepAliveMonitor KeepAliveMonitor { get; }

        public string ClientId { get; }

        public MqttProtocolVersion? ProtocolVersion { get; private set; }

        public bool IsConnected { get; private set; }

        public async Task<bool> RunAsync(MqttConnectPacket connectPacket, IMqttChannelAdapter adapter)
        {
            if (connectPacket == null) throw new ArgumentNullException(nameof(connectPacket));
            if (adapter == null) throw new ArgumentNullException(nameof(adapter));

            try
            {
                _cancellationTokenSource = new CancellationTokenSource();

                _wasCleanDisconnect = false;
                _willMessage = connectPacket.WillMessage;

                IsConnected = true;
                ProtocolVersion = adapter.PacketSerializer.ProtocolVersion;

                PendingMessagesQueue.Start(adapter, _cancellationTokenSource.Token);
                KeepAliveMonitor.Start(connectPacket.KeepAlivePeriod, _cancellationTokenSource.Token);

                await ReceivePacketsAsync(adapter, _cancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (MqttCommunicationException exception)
            {
                _logger.Warning(exception, "Client '{0}': Communication exception while processing client packets.", ClientId);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Client '{0}': Unhandled exception while processing client packets.", ClientId);
            }
            finally
            {
                ProtocolVersion = null;
                IsConnected = false;

                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }

            return _wasCleanDisconnect;
        }

        public void Stop(MqttClientDisconnectType type)
        {
            try
            {
                if (_cancellationTokenSource == null)
                {
                    return;
                }

                _wasCleanDisconnect = type == MqttClientDisconnectType.Clean;

                _cancellationTokenSource?.Cancel(false);
                PendingMessagesQueue.WaitForCompletion();
                KeepAliveMonitor.WaitForCompletion();

                var willMessage = _willMessage;
                _willMessage = null; // clear willmessage so it is send just once

                if (willMessage != null && !_wasCleanDisconnect)
                {
                    _sessionsManager.StartDispatchApplicationMessage(this, willMessage);
                }
            }
            finally
            {
                _logger.Info("Client '{0}': Session stopped.", ClientId);
            }
        }

        public async Task EnqueueApplicationMessageAsync(MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            var result = await SubscriptionsManager.CheckSubscriptionsAsync(applicationMessage).ConfigureAwait(false);
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

            SubscriptionsManager.Subscribe(new MqttSubscribePacket
            {
                TopicFilters = topicFilters
            });

            await EnqueueSubscribedRetainedMessagesAsync(topicFilters).ConfigureAwait(false);
        }

        public Task UnsubscribeAsync(IList<string> topicFilters)
        {
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            SubscriptionsManager.Unsubscribe(new MqttUnsubscribePacket
            {
                TopicFilters = topicFilters
            });

            return Task.FromResult(0);
        }

        public void Dispose()
        {
            SubscriptionsManager?.Dispose();
            PendingMessagesQueue?.Dispose();

            _cancellationTokenSource?.Dispose();
        }

        private void StopDueToKeepAliveTimeout()
        {
            _logger.Info("Client '{0}': Timeout while waiting for KeepAlive packet.", ClientId);
            Stop(MqttClientDisconnectType.NotClean);
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
            catch (Exception exception)
            {
                if (exception is MqttCommunicationException)
                {
                    _logger.Warning(exception, "Client '{0}': Communication exception while processing client packets.", ClientId);
                }
                else
                {
                    _logger.Error(exception, "Client '{0}': Unhandled exception while processing client packets.", ClientId);
                }

                Stop(MqttClientDisconnectType.NotClean);
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
                return adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, new[] { new MqttPingRespPacket() }, cancellationToken);
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

                return adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, new[] { responsePacket }, cancellationToken);
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

            if (packet is MqttDisconnectPacket)
            {
                Stop(MqttClientDisconnectType.Clean);
                return Task.FromResult(0);
            }

            if (packet is MqttConnectPacket)
            {
                Stop(MqttClientDisconnectType.NotClean);
                return Task.FromResult(0);
            }

            _logger.Warning(null, "Client '{0}': Received not supported packet ({1}). Closing connection.", ClientId, packet);
            Stop(MqttClientDisconnectType.NotClean);

            return Task.FromResult(0);
        }

        private async Task EnqueueSubscribedRetainedMessagesAsync(ICollection<TopicFilter> topicFilters)
        {
            var retainedMessages = await _retainedMessagesManager.GetSubscribedMessagesAsync(topicFilters);
            foreach (var applicationMessage in retainedMessages)
            {
                await EnqueueApplicationMessageAsync(applicationMessage).ConfigureAwait(false);
            }
        }

        private async Task HandleIncomingSubscribePacketAsync(IMqttChannelAdapter adapter, MqttSubscribePacket subscribePacket, CancellationToken cancellationToken)
        {
            var subscribeResult = SubscriptionsManager.Subscribe(subscribePacket);
            await adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, new[] { subscribeResult.ResponsePacket }, cancellationToken).ConfigureAwait(false);

            if (subscribeResult.CloseConnection)
            {
                Stop(MqttClientDisconnectType.NotClean);
                return;
            }

            await EnqueueSubscribedRetainedMessagesAsync(subscribePacket.TopicFilters).ConfigureAwait(false);
        }

        private async Task HandleIncomingUnsubscribePacketAsync(IMqttChannelAdapter adapter, MqttUnsubscribePacket unsubscribePacket, CancellationToken cancellationToken)
        {
            var unsubscribeResult = SubscriptionsManager.Unsubscribe(unsubscribePacket);
            await adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, new[] { unsubscribeResult }, cancellationToken);
        }

        private Task HandleIncomingPublishPacketAsync(IMqttChannelAdapter adapter, MqttPublishPacket publishPacket, CancellationToken cancellationToken)
        {
            var applicationMessage = publishPacket.ToApplicationMessage();

            switch (applicationMessage.QualityOfServiceLevel)
            {
                case MqttQualityOfServiceLevel.AtMostOnce:
                    {
                        _sessionsManager.StartDispatchApplicationMessage(this, applicationMessage);
                        return Task.FromResult(0);
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
            _sessionsManager.StartDispatchApplicationMessage(this, applicationMessage);

            var response = new MqttPubAckPacket { PacketIdentifier = publishPacket.PacketIdentifier };
            await adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, new[] { response }, cancellationToken).ConfigureAwait(false);
        }

        private async Task HandleIncomingPublishPacketWithQoS2(IMqttChannelAdapter adapter, MqttApplicationMessage applicationMessage, MqttPublishPacket publishPacket, CancellationToken cancellationToken)
        {
            // QoS 2 is implement as method "B" [4.3.3 QoS 2: Exactly once delivery]
            _sessionsManager.StartDispatchApplicationMessage(this, applicationMessage);

            var response = new MqttPubRecPacket { PacketIdentifier = publishPacket.PacketIdentifier };
            await adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, new[] { response }, cancellationToken).ConfigureAwait(false);
        }

        private Task HandleIncomingPubRelPacketAsync(IMqttChannelAdapter adapter, MqttPubRelPacket pubRelPacket, CancellationToken cancellationToken)
        {
            var response = new MqttPubCompPacket { PacketIdentifier = pubRelPacket.PacketIdentifier };
            return adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, new[] { response }, cancellationToken);
        }
    }
}
