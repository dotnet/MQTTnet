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
        private readonly MqttClientMessageQueue _messageQueue;
        private readonly Action<MqttClientSession, MqttPublishPacket> _publishPacketReceivedCallback;
        private readonly MqttServerOptions _options;

        private CancellationTokenSource _cancellationTokenSource;
        private IMqttCommunicationAdapter _adapter;
        private string _identifier;
        private MqttApplicationMessage _willApplicationMessage;

        public MqttClientSession(string clientId, MqttServerOptions options, Action<MqttClientSession, MqttPublishPacket> publishPacketReceivedCallback)
        {
            ClientId = clientId;
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _publishPacketReceivedCallback = publishPacketReceivedCallback ?? throw new ArgumentNullException(nameof(publishPacketReceivedCallback));

            _messageQueue = new MqttClientMessageQueue(options);
        }

        public string ClientId { get; }

        public bool IsConnected => _adapter != null;

        public async Task RunAsync(string identifier, MqttApplicationMessage willApplicationMessage, IMqttCommunicationAdapter adapter)
        {
            if (adapter == null) throw new ArgumentNullException(nameof(adapter));

            _willApplicationMessage = willApplicationMessage;

            try
            {
                _identifier = identifier;
                _adapter = adapter;
                _cancellationTokenSource = new CancellationTokenSource();

                _messageQueue.Start(adapter);
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    var packet = await adapter.ReceivePacketAsync(TimeSpan.Zero);
                    await HandleIncomingPacketAsync(packet);
                }
            }
            catch (MqttCommunicationException)
            {
            }
            catch (Exception exception)
            {
                MqttTrace.Error(nameof(MqttClientSession), exception, $"Client '{_identifier}': Unhandled exception while processing client packets.");
            }
            finally
            {
                if (willApplicationMessage != null)
                {
                    _publishPacketReceivedCallback(this, _willApplicationMessage.ToPublishPacket());
                }

                _messageQueue.Stop();
                _cancellationTokenSource.Cancel();
                _adapter = null;

                MqttTrace.Information(nameof(MqttClientSession), $"Client '{_identifier}': Disconnected.");
            }
        }

        public void EnqueuePublishPacket(MqttPublishPacket publishPacket)
        {
            if (publishPacket == null) throw new ArgumentNullException(nameof(publishPacket));

            if (!_subscriptionsManager.IsSubscribed(publishPacket))
            {
                return;
            }

            _messageQueue.Enqueue(publishPacket);
            MqttTrace.Verbose(nameof(MqttClientSession), $"Client '{_identifier}: Enqueued pending publish packet.");
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }

        private Task HandleIncomingPacketAsync(MqttBasePacket packet)
        {
            if (packet is MqttSubscribePacket subscribePacket)
            {
                return _adapter.SendPacketAsync(_subscriptionsManager.Subscribe(subscribePacket), _options.DefaultCommunicationTimeout);
            }

            if (packet is MqttUnsubscribePacket unsubscribePacket)
            {
                return _adapter.SendPacketAsync(_subscriptionsManager.Unsubscribe(unsubscribePacket), _options.DefaultCommunicationTimeout);
            }

            if (packet is MqttPublishPacket publishPacket)
            {
                return HandleIncomingPublishPacketAsync(publishPacket);
            }

            if (packet is MqttPubRelPacket pubRelPacket)
            {
                return HandleIncomingPubRelPacketAsync(pubRelPacket);
            }

            if (packet is MqttPubRecPacket pubRecPacket)
            {
                return _adapter.SendPacketAsync(pubRecPacket.CreateResponse<MqttPubRelPacket>(), _options.DefaultCommunicationTimeout);
            }

            if (packet is MqttPubAckPacket || packet is MqttPubCompPacket)
            {
                // Discard message.
                return Task.FromResult((object)null);
            }

            if (packet is MqttPingReqPacket)
            {
                return _adapter.SendPacketAsync(new MqttPingRespPacket(), _options.DefaultCommunicationTimeout);
            }

            if (packet is MqttDisconnectPacket || packet is MqttConnectPacket)
            {
                _cancellationTokenSource.Cancel();
                return Task.FromResult((object)null);
            }

            MqttTrace.Warning(nameof(MqttClientSession), $"Client '{_identifier}': Received not supported packet ({packet}). Closing connection.");
            _cancellationTokenSource.Cancel();

            return Task.FromResult((object)null);
        }

        private Task HandleIncomingPublishPacketAsync(MqttPublishPacket publishPacket)
        {
            if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtMostOnce)
            {
                _publishPacketReceivedCallback(this, publishPacket);
            }
            else if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtLeastOnce)
            {
                _publishPacketReceivedCallback(this, publishPacket);
                return _adapter.SendPacketAsync(new MqttPubAckPacket { PacketIdentifier = publishPacket.PacketIdentifier }, _options.DefaultCommunicationTimeout);
            }
            else if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.ExactlyOnce)
            {
                // QoS 2 is implement as method "B" [4.3.3 QoS 2: Exactly once delivery]
                lock (_unacknowledgedPublishPackets)
                {
                    _unacknowledgedPublishPackets.Add(publishPacket.PacketIdentifier);
                }

                _publishPacketReceivedCallback(this, publishPacket);

                return _adapter.SendPacketAsync(new MqttPubRecPacket { PacketIdentifier = publishPacket.PacketIdentifier }, _options.DefaultCommunicationTimeout);
            }

            throw new MqttCommunicationException("Received not supported QoS level.");
        }

        private Task HandleIncomingPubRelPacketAsync(MqttPubRelPacket pubRelPacket)
        {
            lock (_unacknowledgedPublishPackets)
            {
                _unacknowledgedPublishPackets.Remove(pubRelPacket.PacketIdentifier);
            }

            return _adapter.SendPacketAsync(new MqttPubCompPacket { PacketIdentifier = pubRelPacket.PacketIdentifier }, _options.DefaultCommunicationTimeout);
        }
    }
}
