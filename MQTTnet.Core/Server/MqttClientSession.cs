using System;
using System.Collections.Concurrent;
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
    public class MqttClientSession
    {
        private readonly ConcurrentDictionary<ushort, MqttPublishPacket> _pendingIncomingPublications = new ConcurrentDictionary<ushort, MqttPublishPacket>();

        private readonly MqttClientSubscriptionsManager _subscriptionsManager = new MqttClientSubscriptionsManager();
        private readonly MqttOutgoingPublicationsManager _outgoingPublicationsManager;
        private readonly Action<MqttClientSession, MqttPublishPacket> _publishPacketReceivedCallback;
        private readonly MqttServerOptions _options;

        private CancellationTokenSource _cancellationTokenSource;
        private IMqttCommunicationAdapter _adapter;
        private string _identifier;
        private MqttApplicationMessage _willApplicationMessage;
        
        public MqttClientSession(MqttServerOptions options, Action<MqttClientSession, MqttPublishPacket> publishPacketReceivedCallback)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _publishPacketReceivedCallback = publishPacketReceivedCallback ?? throw new ArgumentNullException(nameof(publishPacketReceivedCallback));
            
            _outgoingPublicationsManager = new MqttOutgoingPublicationsManager(options);
        }

        public async Task RunAsync(string identifier, MqttApplicationMessage willApplicationMessage, IMqttCommunicationAdapter adapter)
        {
            if (adapter == null) throw new ArgumentNullException(nameof(adapter));

            _willApplicationMessage = willApplicationMessage;

            try
            {
                _identifier = identifier;
                _adapter = adapter;
                _cancellationTokenSource = new CancellationTokenSource();

                _outgoingPublicationsManager.Start(adapter);
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
                
                _outgoingPublicationsManager.Stop();
                _cancellationTokenSource.Cancel();
                _adapter = null;

                MqttTrace.Information(nameof(MqttClientSession), $"Client '{_identifier}': Disconnected.");
            }
        }

        public void DispatchPublishPacket(MqttClientSession senderClientSession, MqttPublishPacket publishPacket)
        {
            if (senderClientSession == null) throw new ArgumentNullException(nameof(senderClientSession));
            if (publishPacket == null) throw new ArgumentNullException(nameof(publishPacket));

            if (!_subscriptionsManager.IsTopicSubscribed(publishPacket))
            {
                return;
            }

            _outgoingPublicationsManager.Enqueue(senderClientSession, publishPacket);
            MqttTrace.Verbose(nameof(MqttClientSession), $"Client '{_identifier}: Enqueued pending publish packet.");
        }

        private async Task HandleIncomingPacketAsync(MqttBasePacket packet)
        {
            var subscribePacket = packet as MqttSubscribePacket;
            if (subscribePacket != null)
            {
                await _adapter.SendPacketAsync(_subscriptionsManager.Subscribe(subscribePacket), _options.DefaultCommunicationTimeout);
                return;
            }

            var unsubscribePacket = packet as MqttUnsubscribePacket;
            if (unsubscribePacket != null)
            {
                await _adapter.SendPacketAsync(_subscriptionsManager.Unsubscribe(unsubscribePacket), _options.DefaultCommunicationTimeout);
                return;
            }

            var publishPacket = packet as MqttPublishPacket;
            if (publishPacket != null)
            {
                await HandleIncomingPublishPacketAsync(publishPacket);
                return;
            }

            var pubRelPacket = packet as MqttPubRelPacket;
            if (pubRelPacket != null)
            {
                await HandleIncomingPubRelPacketAsync(pubRelPacket);
                return;
            }

            var pubAckPacket = packet as MqttPubAckPacket;
            if (pubAckPacket != null)
            {
                await HandleIncomingPubAckPacketAsync(pubAckPacket);
                return;
            }

            if (packet is MqttPingReqPacket)
            {
                await _adapter.SendPacketAsync(new MqttPingRespPacket(), _options.DefaultCommunicationTimeout);
                return;
            }

            if (packet is MqttDisconnectPacket || packet is MqttConnectPacket)
            {
                _cancellationTokenSource.Cancel();
                return;
            }

            MqttTrace.Warning(nameof(MqttClientSession), $"Client '{_identifier}': Received not supported packet ({packet}). Closing connection.");
            _cancellationTokenSource.Cancel();
        }

        private async Task HandleIncomingPubAckPacketAsync(MqttPubAckPacket pubAckPacket)
        {
            await Task.FromResult(0);
        }

        private async Task HandleIncomingPublishPacketAsync(MqttPublishPacket publishPacket)
        {
            if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtMostOnce)
            {
                _publishPacketReceivedCallback(this, publishPacket);
            }
            else if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtLeastOnce)
            {
                await _adapter.SendPacketAsync(new MqttPubAckPacket { PacketIdentifier = publishPacket.PacketIdentifier }, _options.DefaultCommunicationTimeout);
                _publishPacketReceivedCallback(this, publishPacket);
            }
            else if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.ExactlyOnce)
            {
                _pendingIncomingPublications[publishPacket.PacketIdentifier] = publishPacket;
                await _adapter.SendPacketAsync(new MqttPubRecPacket { PacketIdentifier = publishPacket.PacketIdentifier }, _options.DefaultCommunicationTimeout);
            }
        }

        private async Task HandleIncomingPubRelPacketAsync(MqttPubRelPacket pubRelPacket)
        {
            MqttPublishPacket publishPacket;
            if (!_pendingIncomingPublications.TryRemove(pubRelPacket.PacketIdentifier, out publishPacket))
            {
                return;
            }

            await _adapter.SendPacketAsync(new MqttPubCompPacket { PacketIdentifier = publishPacket.PacketIdentifier }, _options.DefaultCommunicationTimeout);
            _publishPacketReceivedCallback(this, publishPacket);
        }
    }
}
