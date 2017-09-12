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

        public bool IsConnected => Adapter != null;

        public IMqttCommunicationAdapter Adapter { get; private set; }

        public async Task RunAsync(string identifier, MqttApplicationMessage willApplicationMessage, IMqttCommunicationAdapter adapter)
        {
            if (adapter == null) throw new ArgumentNullException(nameof(adapter));

            _willApplicationMessage = willApplicationMessage;

            try
            {
                _identifier = identifier;
                Adapter = adapter;
                _cancellationTokenSource = new CancellationTokenSource();

                _messageQueue.Start(adapter);
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    var packet = await adapter.ReceivePacketAsync(TimeSpan.Zero).ConfigureAwait(false);
                    await HandleIncomingPacketAsync(packet).ConfigureAwait(false);
                }
            }
            catch (MqttCommunicationException)
            {
            }
            catch (Exception exception)
            {
                MqttTrace.Error(nameof(MqttClientSession), exception, "Client '{0}': Unhandled exception while processing client packets.", _identifier);
            }
            finally
            {
                if (willApplicationMessage != null)
                {
                    _publishPacketReceivedCallback(this, _willApplicationMessage.ToPublishPacket());
                }

                _messageQueue.Stop();
                _cancellationTokenSource.Cancel();
                Adapter = null;

                MqttTrace.Information(nameof(MqttClientSession), "Client '{0}': Disconnected.", _identifier);
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
            MqttTrace.Verbose(nameof(MqttClientSession), "Client '{0}': Enqueued pending publish packet.", _identifier);
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
                return Adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, _subscriptionsManager.Subscribe(subscribePacket));
            }

            if (packet is MqttUnsubscribePacket unsubscribePacket)
            {
                return Adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, _subscriptionsManager.Unsubscribe(unsubscribePacket));
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
                return Adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, pubRecPacket.CreateResponse<MqttPubRelPacket>());
            }

            if (packet is MqttPubAckPacket || packet is MqttPubCompPacket)
            {
                // Discard message.
                return Task.FromResult((object)null);
            }

            if (packet is MqttPingReqPacket)
            {
                return Adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, new MqttPingRespPacket());
            }

            if (packet is MqttDisconnectPacket || packet is MqttConnectPacket)
            {
                _cancellationTokenSource.Cancel();
                return Task.FromResult((object)null);
            }

            MqttTrace.Warning(nameof(MqttClientSession), "Client '{0}': Received not supported packet ({1}). Closing connection.", _identifier, packet);
            _cancellationTokenSource.Cancel();

            return Task.FromResult((object)null);
        }

        private Task HandleIncomingPublishPacketAsync(MqttPublishPacket publishPacket)
        {
            if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtMostOnce)
            {
                _publishPacketReceivedCallback(this, publishPacket);
                return Task.FromResult(0);
            }

            if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtLeastOnce)
            {
                _publishPacketReceivedCallback(this, publishPacket);
                return Adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, new MqttPubAckPacket { PacketIdentifier = publishPacket.PacketIdentifier });
            }

            if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.ExactlyOnce)
            {
                // QoS 2 is implement as method "B" [4.3.3 QoS 2: Exactly once delivery]
                lock (_unacknowledgedPublishPackets)
                {
                    _unacknowledgedPublishPackets.Add(publishPacket.PacketIdentifier);
                }

                _publishPacketReceivedCallback(this, publishPacket);

                return Adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, new MqttPubRecPacket { PacketIdentifier = publishPacket.PacketIdentifier });
            }

            throw new MqttCommunicationException("Received a not supported QoS level.");
        }

        private Task HandleIncomingPubRelPacketAsync(MqttPubRelPacket pubRelPacket)
        {
            lock (_unacknowledgedPublishPackets)
            {
                _unacknowledgedPublishPackets.Remove(pubRelPacket.PacketIdentifier);
            }

            return Adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, new MqttPubCompPacket { PacketIdentifier = pubRelPacket.PacketIdentifier });
        }
    }
}
