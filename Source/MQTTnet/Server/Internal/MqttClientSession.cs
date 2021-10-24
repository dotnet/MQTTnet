using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server.Internal
{
    public sealed class MqttClientSession : IDisposable
    {
        readonly MqttPacketBus _packetBus = new MqttPacketBus();

        readonly Dictionary<ushort, MqttPublishPacket> _publishPacketsWithoutAcknowledge = new Dictionary<ushort, MqttPublishPacket>();

        readonly IMqttServerOptions _serverOptions;
        readonly MqttClientSessionsManager _clientSessionsManager;

        public MqttClientSession(string clientId,
            IDictionary<object, object> items,
            MqttServerEventDispatcher eventDispatcher,
            IMqttServerOptions serverOptions,
            IMqttRetainedMessagesManager retainedMessagesManager,
            MqttClientSessionsManager clientSessionsManager)
        {
            _serverOptions = serverOptions ?? throw new ArgumentNullException(nameof(serverOptions));
            _clientSessionsManager = clientSessionsManager ?? throw new ArgumentNullException(nameof(clientSessionsManager));
            Id = clientId ?? throw new ArgumentNullException(nameof(clientId));
            Items = items ?? throw new ArgumentNullException(nameof(items));

            SubscriptionsManager = new MqttClientSubscriptionsManager(this, serverOptions, eventDispatcher, retainedMessagesManager);
        }

        public event EventHandler Deleted;
        
        public string Id { get; }

        public MqttPacketIdentifierProvider PacketIdentifierProvider { get; } = new MqttPacketIdentifierProvider();

        public DateTime CreatedTimestamp { get; } = DateTime.UtcNow;
        
        //public bool IsCleanSession { get; set; } = true;

        public MqttConnectPacket LatestConnectPacket { get; set; }

        public MqttClientSubscriptionsManager SubscriptionsManager { get; }
        
        public IDictionary<object, object> Items { get; }
        
        public bool WillMessageSent { get; set; }

        public long PendingDataPacketsCount => _packetBus.DataPacketsCount;

        public void AcknowledgePublishPacket(ushort packetIdentifier)
        {
            _publishPacketsWithoutAcknowledge.Remove(packetIdentifier);
        }
        
        public void EnqueuePacket(MqttPacketBusItem packetBusItem)
        {
            if (packetBusItem == null) throw new ArgumentNullException(nameof(packetBusItem));

            if (_packetBus.PacketsCount >= _serverOptions.MaxPendingMessagesPerClient)
            {
                if (_serverOptions.PendingMessagesOverflowStrategy == MqttPendingMessagesOverflowStrategy.DropNewMessage)
                {
                    // TODO: Log!
                    return;
                }
                else
                {
                    // TODO: Implement.
                }
            }
            
            if (packetBusItem.Packet is MqttPublishPacket publishPacket)
            {
                if (publishPacket.QualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce)
                {
                    _publishPacketsWithoutAcknowledge[publishPacket.PacketIdentifier] = publishPacket;
                }
                
                _packetBus.Enqueue(packetBusItem, MqttPacketBusPartition.Data);
            }
            else if (packetBusItem.Packet is MqttPingReqPacket || packetBusItem.Packet is MqttPingRespPacket)
            {
                _packetBus.Enqueue(packetBusItem, MqttPacketBusPartition.Health);    
            }
            else
            {
                _packetBus.Enqueue(packetBusItem, MqttPacketBusPartition.Control);
            }
        }

        public Task<MqttPacketBusItem> DequeuePacketAsync(CancellationToken cancellationToken)
        {
            return _packetBus.DequeueAsync(cancellationToken);
        }
        
        public Task DeleteAsync()
        {
            return _clientSessionsManager.DeleteSessionAsync(Id);
        }
        
        public void OnDeleted()
        {
            Deleted?.Invoke(this, EventArgs.Empty);
        }
        
        public void Recover()
        {
            // TODO: Keep the bus and only insert pending items again.
            // TODO: Check if packet identifier must be restarted or not.
            _packetBus.Clear();

            foreach (var publishPacket in _publishPacketsWithoutAcknowledge.Values.ToList())
            {
                EnqueuePacket(new MqttPacketBusItem(publishPacket));
            }
        }

        public void Dispose()
        {
            _packetBus?.Dispose();
        }
    }
}