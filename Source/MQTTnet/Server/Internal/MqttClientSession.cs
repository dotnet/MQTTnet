using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client;
using MQTTnet.Internal;
using MQTTnet.Packets;

namespace MQTTnet.Server.Internal
{
    public sealed class MqttClientSession : IDisposable
    {
        readonly MqttPacketBus _packetBus = new MqttPacketBus();

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
            ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            Items = items ?? throw new ArgumentNullException(nameof(items));

            SubscriptionsManager = new MqttClientSubscriptionsManager(this, serverOptions, eventDispatcher, retainedMessagesManager);
            //ApplicationMessagesQueue = new MqttClientSessionApplicationMessagesQueue(serverOptions);
        }

        public event EventHandler Deleted;
        
        public string ClientId { get; }

        public MqttPacketIdentifierProvider PacketIdentifierProvider { get; } = new MqttPacketIdentifierProvider();

        public DateTime CreatedTimestamp { get; } = DateTime.UtcNow;
        
        public bool IsCleanSession { get; set; } = true;

        public MqttApplicationMessage WillMessage { get; set; }

        public MqttClientSubscriptionsManager SubscriptionsManager { get; }
        
        //public MqttClientSessionApplicationMessagesQueue ApplicationMessagesQueue { get; }

        public IDictionary<object, object> Items { get; }
        
        public void EnqueuePacket(MqttPacketBusItem packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

            if (_packetBus.PacketsCount >= _serverOptions.MaxPendingMessagesPerClient)
            {
                // TODO: Log!
                return;
            }
            
            if (packet.Packet is MqttPublishPacket)
            {
                _packetBus.Enqueue(packet, MqttPacketBusPartition.Data);    
            }
            else if (packet.Packet is MqttPingReqPacket || packet.Packet is MqttPingRespPacket)
            {
                _packetBus.Enqueue(packet, MqttPacketBusPartition.Health);    
            }
            else
            {
                _packetBus.Enqueue(packet, MqttPacketBusPartition.Control);
            }
        }

        public Task<MqttPacketBusItem> DequeuePacketAsync(CancellationToken cancellationToken)
        {
            return _packetBus.DequeueAsync(cancellationToken);
        }
        
        public Task DeleteAsync()
        {
            return _clientSessionsManager.DeleteSessionAsync(ClientId);
        }
        
        public void OnDeleted()
        {
            Deleted?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            _packetBus?.Dispose();
        }
    }
}