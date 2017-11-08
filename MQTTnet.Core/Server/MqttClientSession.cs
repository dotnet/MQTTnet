using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Exceptions;
using MQTTnet.Core.Internal;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;
using MQTTnet.Core.Serializer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MQTTnet.Core.Server
{
    public sealed class MqttClientSession
    {
        private readonly HashSet<ushort> _unacknowledgedPublishPackets = new HashSet<ushort>();

        private readonly IMqttClientRetainedMessageManager _clientRetainedMessageManager;
        private readonly MqttClientSubscriptionsManager _subscriptionsManager;
        private readonly MqttClientSessionsManager _sessionsManager;
        private readonly MqttClientPendingMessagesQueue _pendingMessagesQueue;
        private readonly MqttServerOptions _options;
        private readonly ILogger<MqttClientSession> _logger;

        private IMqttCommunicationAdapter _adapter;
        private CancellationTokenSource _cancellationTokenSource;
        private MqttApplicationMessage _willMessage;

        public MqttClientSession(
            string clientId, 
            IOptions<MqttServerOptions> options,
            MqttClientSessionsManager sessionsManager,
            MqttClientSubscriptionsManager subscriptionsManager,
            ILogger<MqttClientSession> logger, 
            ILogger<MqttClientPendingMessagesQueue> messageQueueLogger,
            IMqttClientRetainedMessageManager clientRetainedMessageManager)
        {
            _clientRetainedMessageManager = clientRetainedMessageManager ?? throw new ArgumentNullException(nameof(clientRetainedMessageManager));
            _sessionsManager = sessionsManager ?? throw new ArgumentNullException(nameof(sessionsManager));
            _subscriptionsManager = subscriptionsManager ?? throw new ArgumentNullException(nameof(subscriptionsManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            ClientId = clientId;

            _options = options.Value;
            _pendingMessagesQueue = new MqttClientPendingMessagesQueue(_options, this, messageQueueLogger);
        }

        public string ClientId { get; }

        public MqttProtocolVersion? ProtocolVersion => _adapter?.PacketSerializer.ProtocolVersion;

        public bool IsConnected => _adapter != null;

        public async Task RunAsync(MqttApplicationMessage willMessage, IMqttCommunicationAdapter adapter)
        {
            if (adapter == null) throw new ArgumentNullException(nameof(adapter));

            try
            {
                _willMessage = willMessage;
                _adapter = adapter;
                _cancellationTokenSource = new CancellationTokenSource();

                _pendingMessagesQueue.Start(adapter, _cancellationTokenSource.Token);
                await ReceivePacketsAsync(adapter, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (MqttCommunicationException exception)
            {
                _logger.LogWarning(new EventId(), exception, "Client '{0}': Communication exception while processing client packets.", ClientId);
            }
            catch (Exception exception)
            {
                _logger.LogError(new EventId(), exception, "Client '{0}': Unhandled exception while processing client packets.", ClientId);
            }
        }

        public void Stop()
        {
            try
            {
                _cancellationTokenSource?.Cancel(false);
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                _adapter = null;

                _logger.LogInformation("Client '{0}': Disconnected.", ClientId);
            }
            finally
            {
                if (_willMessage != null)
                {
                    _sessionsManager.DispatchApplicationMessage(this, _willMessage);
                }
            }
        }

        public void EnqueuePublishPacket(MqttPublishPacket publishPacket)
        {
            if (publishPacket == null) throw new ArgumentNullException(nameof(publishPacket));

            if (!_subscriptionsManager.IsSubscribed(publishPacket))
            {
                return;
            }

            _pendingMessagesQueue.Enqueue(publishPacket);
        }

        private async Task ReceivePacketsAsync(IMqttCommunicationAdapter adapter, CancellationToken cancellationToken)
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
                _logger.LogWarning(new EventId(), exception, "Client '{0}': Communication exception while processing client packets.", ClientId);
                Stop();
            }
            catch (Exception exception)
            {
                _logger.LogError(new EventId(), exception, "Client '{0}': Unhandled exception while processing client packets.", ClientId);
                Stop();
            }
        }

        private Task ProcessReceivedPacketAsync(IMqttCommunicationAdapter adapter, MqttBasePacket packet, CancellationToken cancellationToken)
        {
            if (packet is MqttPingReqPacket)
            {
                return adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, cancellationToken, new MqttPingRespPacket());
            }

            if (packet is MqttPublishPacket publishPacket)
            {
                return HandleIncomingPublishPacketAsync(adapter, publishPacket, cancellationToken);
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
                Stop();
                return Task.FromResult(0);
            }

            _logger.LogWarning("Client '{0}': Received not supported packet ({1}). Closing connection.", ClientId, packet);
            Stop();

            return Task.FromResult(0);
        }

        private async Task HandleIncomingSubscribePacketAsync(IMqttCommunicationAdapter adapter, MqttSubscribePacket subscribePacket, CancellationToken cancellationToken)
        {
            var subscribeResult = _subscriptionsManager.Subscribe(subscribePacket, ClientId);

            await adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, cancellationToken, subscribeResult.ResponsePacket);
            await EnqueueSubscribedRetainedMessagesAsync(subscribePacket);

            if (subscribeResult.CloseConnection)
            {
                await adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, cancellationToken, new MqttDisconnectPacket());
                Stop();
            }
        }

        private async Task EnqueueSubscribedRetainedMessagesAsync(MqttSubscribePacket subscribePacket)
        {
            var retainedMessages = await _clientRetainedMessageManager.GetSubscribedMessagesAsync(subscribePacket);
            foreach (var publishPacket in retainedMessages)
            {
                EnqueuePublishPacket(publishPacket.ToPublishPacket());
            }
        }

        private async Task HandleIncomingPublishPacketAsync(IMqttCommunicationAdapter adapter, MqttPublishPacket publishPacket, CancellationToken cancellationToken)
        {
            var applicationMessage = publishPacket.ToApplicationMessage();

            var interceptorContext = new MqttApplicationMessageInterceptorContext
            {
                ApplicationMessage = applicationMessage
            };

            _options.ApplicationMessageInterceptor?.Invoke(interceptorContext);
            applicationMessage = interceptorContext.ApplicationMessage;

            if (applicationMessage.Retain)
            {
                await _clientRetainedMessageManager.HandleMessageAsync(ClientId, applicationMessage);
            }

            switch (applicationMessage.QualityOfServiceLevel)
            {
                case MqttQualityOfServiceLevel.AtMostOnce:
                    {
                        _sessionsManager.DispatchApplicationMessage(this, applicationMessage);
                        return;
                    }
                case MqttQualityOfServiceLevel.AtLeastOnce:
                    {
                        _sessionsManager.DispatchApplicationMessage(this, applicationMessage);

                        await adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, cancellationToken,
                            new MqttPubAckPacket { PacketIdentifier = publishPacket.PacketIdentifier });

                        return;
                    }
                case MqttQualityOfServiceLevel.ExactlyOnce:
                    {
                        // QoS 2 is implement as method "B" [4.3.3 QoS 2: Exactly once delivery]
                        lock (_unacknowledgedPublishPackets)
                        {
                            _unacknowledgedPublishPackets.Add(publishPacket.PacketIdentifier);
                        }

                        _sessionsManager.DispatchApplicationMessage(this, applicationMessage);

                        await adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, cancellationToken,
                            new MqttPubRecPacket { PacketIdentifier = publishPacket.PacketIdentifier });

                        return;
                    }
                default:
                    throw new MqttCommunicationException("Received a not supported QoS level.");
            }
        }

        private Task HandleIncomingPubRelPacketAsync(IMqttCommunicationAdapter adapter, MqttPubRelPacket pubRelPacket, CancellationToken cancellationToken)
        {
            lock (_unacknowledgedPublishPackets)
            {
                _unacknowledgedPublishPackets.Remove(pubRelPacket.PacketIdentifier);
            }

            return adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, cancellationToken, new MqttPubCompPacket { PacketIdentifier = pubRelPacket.PacketIdentifier });
        }
    }
}
