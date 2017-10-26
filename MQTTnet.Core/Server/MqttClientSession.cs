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

namespace MQTTnet.Core.Server
{
    public sealed class MqttClientSession : IDisposable
    {
        private readonly HashSet<ushort> _unacknowledgedPublishPackets = new HashSet<ushort>();

        private readonly MqttClientSubscriptionsManager _subscriptionsManager = new MqttClientSubscriptionsManager();
        private readonly MqttClientSessionsManager _mqttClientSessionsManager;
        private readonly MqttClientPendingMessagesQueue _pendingMessagesQueue;
        private readonly MqttServerOptions _options;
        private readonly ILogger<MqttClientSession> _logger;

        private IMqttCommunicationAdapter _adapter;
        private CancellationTokenSource _cancellationTokenSource;
        private MqttApplicationMessage _willMessage;

        public MqttClientSession(string clientId, MqttClientSessionsManager mqttClientSessionsManager, ILogger<MqttClientSession> logger, ILogger<MqttClientPendingMessagesQueue> msgQueueLogger)
        {
            _mqttClientSessionsManager = mqttClientSessionsManager ?? throw new ArgumentNullException(nameof(mqttClientSessionsManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ClientId = clientId;

            _options = mqttClientSessionsManager.Options;
            _pendingMessagesQueue = new MqttClientPendingMessagesQueue(mqttClientSessionsManager.Options, this, msgQueueLogger);
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
                    _mqttClientSessionsManager.DispatchApplicationMessage(this, _willMessage);
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
            _logger.LogTrace("Client '{0}': Enqueued pending publish packet.", ClientId);
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }

        private async Task ReceivePacketsAsync(IMqttCommunicationAdapter adapter, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var packet = await adapter.ReceivePacketAsync(TimeSpan.Zero, cancellationToken).ConfigureAwait(false);
                    await ProcessReceivedPacketAsync(adapter, packet).ConfigureAwait(false);
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

        private async Task ProcessReceivedPacketAsync(IMqttCommunicationAdapter adapter, MqttBasePacket packet)
        {
            if (packet is MqttSubscribePacket subscribePacket)
            {
                await adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, _cancellationTokenSource.Token, _subscriptionsManager.Subscribe(subscribePacket));
                EnqueueRetainedMessages(subscribePacket);
            }
            else if (packet is MqttUnsubscribePacket unsubscribePacket)
            {
                await adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, _cancellationTokenSource.Token, _subscriptionsManager.Unsubscribe(unsubscribePacket));
            }
            else if (packet is MqttPublishPacket publishPacket)
            {
                await HandleIncomingPublishPacketAsync(adapter, publishPacket);
            }
            else if (packet is MqttPubRelPacket pubRelPacket)
            {
                await HandleIncomingPubRelPacketAsync(adapter, pubRelPacket);
            }
            else if (packet is MqttPubRecPacket pubRecPacket)
            {
                await adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, _cancellationTokenSource.Token, pubRecPacket.CreateResponse<MqttPubRelPacket>());
            }
            else if (packet is MqttPubAckPacket || packet is MqttPubCompPacket)
            {
                // Discard message.
            }
            else if (packet is MqttPingReqPacket)
            {
                await adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, _cancellationTokenSource.Token, new MqttPingRespPacket());
            }
            else if (packet is MqttDisconnectPacket || packet is MqttConnectPacket)
            {
                Stop();
            }
            else
            {
                _logger.LogWarning("Client '{0}': Received not supported packet ({1}). Closing connection.", ClientId, packet);
                Stop();
            }
        }

        private void EnqueueRetainedMessages(MqttSubscribePacket subscribePacket)
        {
            var retainedMessages = _mqttClientSessionsManager.RetainedMessagesManager.GetMessages(subscribePacket);
            foreach (var publishPacket in retainedMessages)
            {
                EnqueuePublishPacket(publishPacket.ToPublishPacket());
            }
        }

        private async Task HandleIncomingPublishPacketAsync(IMqttCommunicationAdapter adapter, MqttPublishPacket publishPacket)
        {
            var applicationMessage = publishPacket.ToApplicationMessage();
            _options.ApplicationMessageInterceptor?.Invoke(applicationMessage);

            if (applicationMessage.Retain)
            {
                await _mqttClientSessionsManager.RetainedMessagesManager.HandleMessageAsync(ClientId, applicationMessage);
            }

            switch (applicationMessage.QualityOfServiceLevel)
            {
                case MqttQualityOfServiceLevel.AtMostOnce:
                    {
                        _mqttClientSessionsManager.DispatchApplicationMessage(this, applicationMessage);
                        return;
                    }
                case MqttQualityOfServiceLevel.AtLeastOnce:
                    {
                        _mqttClientSessionsManager.DispatchApplicationMessage(this, applicationMessage);

                        await adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, _cancellationTokenSource.Token,
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

                        _mqttClientSessionsManager.DispatchApplicationMessage(this, applicationMessage);

                        await adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, _cancellationTokenSource.Token,
                            new MqttPubRecPacket { PacketIdentifier = publishPacket.PacketIdentifier });

                        return;
                    }
                default:
                    throw new MqttCommunicationException("Received a not supported QoS level.");
            }
        }

        private Task HandleIncomingPubRelPacketAsync(IMqttCommunicationAdapter adapter, MqttPubRelPacket pubRelPacket)
        {
            lock (_unacknowledgedPublishPackets)
            {
                _unacknowledgedPublishPackets.Remove(pubRelPacket.PacketIdentifier);
            }

            return adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, _cancellationTokenSource.Token, new MqttPubCompPacket { PacketIdentifier = pubRelPacket.PacketIdentifier });
        }
    }
}
