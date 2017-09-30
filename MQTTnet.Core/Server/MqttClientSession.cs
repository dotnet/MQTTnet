using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Exceptions;
using MQTTnet.Core.Internal;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;

namespace MQTTnet.Core.Server
{
    public sealed class MqttClientSession : IDisposable
    {
        private readonly HashSet<ushort> _unacknowledgedPublishPackets = new HashSet<ushort>();
        
        private readonly MqttClientSubscriptionsManager _subscriptionsManager = new MqttClientSubscriptionsManager();
        private readonly MqttClientSessionsManager _mqttClientSessionsManager;
        private readonly MqttClientPendingMessagesQueue _pendingMessagesQueue;
        private readonly MqttServerOptions _options;

        private string _identifier;
        private CancellationTokenSource _cancellationTokenSource;
        private MqttApplicationMessage _willMessage;

        public MqttClientSession(string clientId, MqttServerOptions options, MqttClientSessionsManager mqttClientSessionsManager)
        {
            ClientId = clientId;
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _mqttClientSessionsManager = mqttClientSessionsManager ?? throw new ArgumentNullException(nameof(mqttClientSessionsManager));
            _pendingMessagesQueue = new MqttClientPendingMessagesQueue(options, this);
        }

        public string ClientId { get; }

        public bool IsConnected => Adapter != null;

        public IMqttCommunicationAdapter Adapter { get; private set; }

        public async Task RunAsync(string identifier, MqttApplicationMessage willMessage, IMqttCommunicationAdapter adapter)
        {
            if (adapter == null) throw new ArgumentNullException(nameof(adapter));

            _willMessage = willMessage;

            try
            {
                _identifier = identifier;
                Adapter = adapter;
                _cancellationTokenSource = new CancellationTokenSource();

                _pendingMessagesQueue.Start(adapter, _cancellationTokenSource.Token);
                await ReceivePacketsAsync(adapter, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (MqttCommunicationException exception)
            {
                MqttTrace.Warning(nameof(MqttClientSession), exception, "Client '{0}': Communication exception while processing client packets.", _identifier);
            }
            catch (Exception exception)
            {
                MqttTrace.Error(nameof(MqttClientSession), exception, "Client '{0}': Unhandled exception while processing client packets.", _identifier);
            }
        }

        public void Stop()
        {
            if (_willMessage != null)
            {
                _mqttClientSessionsManager.DispatchPublishPacket(this, _willMessage.ToPublishPacket());
            }

            _cancellationTokenSource?.Cancel(false);
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            Adapter = null;

            MqttTrace.Information(nameof(MqttClientSession), "Client '{0}': Disconnected.", _identifier);
        }

        public void EnqueuePublishPacket(MqttPublishPacket publishPacket)
        {
            if (publishPacket == null) throw new ArgumentNullException(nameof(publishPacket));

            if (!_subscriptionsManager.IsSubscribed(publishPacket))
            {
                return;
            }

            _pendingMessagesQueue.Enqueue(publishPacket);
            MqttTrace.Verbose(nameof(MqttClientSession), "Client '{0}': Enqueued pending publish packet.", _identifier);
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
                    await ProcessReceivedPacketAsync(packet).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (MqttCommunicationException exception)
            {
                MqttTrace.Warning(nameof(MqttClientSession), exception, "Client '{0}': Communication exception while processing client packets.", _identifier);
                Stop();
            }
            catch (Exception exception)
            {
                MqttTrace.Error(nameof(MqttClientSession), exception, "Client '{0}': Unhandled exception while processing client packets.", _identifier);
                Stop();
            }
        }

        private async Task ProcessReceivedPacketAsync(MqttBasePacket packet)
        {
            if (packet is MqttSubscribePacket subscribePacket)
            {
                await Adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, _cancellationTokenSource.Token, _subscriptionsManager.Subscribe(subscribePacket));
                EnqueueRetainedMessages(subscribePacket);
            }
            else if (packet is MqttUnsubscribePacket unsubscribePacket)
            {
                await Adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, _cancellationTokenSource.Token, _subscriptionsManager.Unsubscribe(unsubscribePacket));
            }
            else if (packet is MqttPublishPacket publishPacket)
            {
                await HandleIncomingPublishPacketAsync(publishPacket);
            }
            else if (packet is MqttPubRelPacket pubRelPacket)
            {
                await HandleIncomingPubRelPacketAsync(pubRelPacket);
            }
            else if (packet is MqttPubRecPacket pubRecPacket)
            {
                await Adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, _cancellationTokenSource.Token, pubRecPacket.CreateResponse<MqttPubRelPacket>());
            }
            else if (packet is MqttPubAckPacket || packet is MqttPubCompPacket)
            {
                // Discard message.
            }
            else if (packet is MqttPingReqPacket)
            {
                await Adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, _cancellationTokenSource.Token, new MqttPingRespPacket());
            }
            else if (packet is MqttDisconnectPacket || packet is MqttConnectPacket)
            {
                _cancellationTokenSource.Cancel();
            }
            else
            {
                MqttTrace.Warning(nameof(MqttClientSession), "Client '{0}': Received not supported packet ({1}). Closing connection.", _identifier, packet);
                _cancellationTokenSource.Cancel();
            }
        }

        private void EnqueueRetainedMessages(MqttSubscribePacket subscribePacket)
        {
            var retainedMessages = _mqttClientSessionsManager.RetainedMessagesManager.GetMessages(subscribePacket);
            foreach (var publishPacket in retainedMessages)
            {
                EnqueuePublishPacket(publishPacket);
            }
        }

        private async Task HandleIncomingPublishPacketAsync(MqttPublishPacket publishPacket)
        {
            if (publishPacket.Retain)
            {
                await _mqttClientSessionsManager.RetainedMessagesManager.HandleMessageAsync(_identifier, publishPacket);
            }

            if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtMostOnce)
            {
                _mqttClientSessionsManager.DispatchPublishPacket(this, publishPacket);
                return;
            }

            if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtLeastOnce)
            {
                _mqttClientSessionsManager.DispatchPublishPacket(this, publishPacket);
                await Adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, _cancellationTokenSource.Token, new MqttPubAckPacket { PacketIdentifier = publishPacket.PacketIdentifier });
                return;
            }

            if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.ExactlyOnce)
            {
                // QoS 2 is implement as method "B" [4.3.3 QoS 2: Exactly once delivery]
                lock (_unacknowledgedPublishPackets)
                {
                    _unacknowledgedPublishPackets.Add(publishPacket.PacketIdentifier);
                }

                _mqttClientSessionsManager.DispatchPublishPacket(this, publishPacket);

                await Adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, _cancellationTokenSource.Token, new MqttPubRecPacket { PacketIdentifier = publishPacket.PacketIdentifier });
                return;
            }

            throw new MqttCommunicationException("Received a not supported QoS level.");
        }

        private Task HandleIncomingPubRelPacketAsync(MqttPubRelPacket pubRelPacket)
        {
            lock (_unacknowledgedPublishPackets)
            {
                _unacknowledgedPublishPackets.Remove(pubRelPacket.PacketIdentifier);
            }

            return Adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, _cancellationTokenSource.Token, new MqttPubCompPacket { PacketIdentifier = pubRelPacket.PacketIdentifier });
        }
    }
}
