// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public sealed class MqttSession : IDisposable
    {
        readonly MqttClientSessionsManager _clientSessionsManager;
        readonly MqttPacketBus _packetBus = new MqttPacketBus();
        readonly MqttPacketIdentifierProvider _packetIdentifierProvider = new MqttPacketIdentifierProvider();

        readonly MqttConnectPacket _connectPacket;
        readonly MqttServerOptions _serverOptions;
        readonly MqttClientSubscriptionsManager _subscriptionsManager;

        // Do not use a dictionary in order to keep the ordering of the messages.
        readonly List<MqttPublishPacket> _unacknowledgedPublishPackets = new List<MqttPublishPacket>();

        // Bookkeeping to know if this is a subscribing client; lazy initialize later.
        HashSet<string> _subscribedTopics;

        public MqttSession(
            MqttConnectPacket connectPacket,
            IDictionary items,
            MqttServerOptions serverOptions,
            MqttServerEventContainer eventContainer,
            MqttRetainedMessagesManager retainedMessagesManager,
            MqttClientSessionsManager clientSessionsManager)
        {
            Items = items ?? throw new ArgumentNullException(nameof(items));

            _connectPacket = connectPacket ?? throw new ArgumentNullException(nameof(connectPacket));
            _serverOptions = serverOptions ?? throw new ArgumentNullException(nameof(serverOptions));
            _clientSessionsManager = clientSessionsManager ?? throw new ArgumentNullException(nameof(clientSessionsManager));

            _subscriptionsManager = new MqttClientSubscriptionsManager(this, eventContainer, retainedMessagesManager, clientSessionsManager);
        }

        public DateTime CreatedTimestamp { get; } = DateTime.UtcNow;
        
        public DateTime? DisconnectedTimestamp { get; set; }

        public bool HasSubscribedTopics => _subscribedTopics != null && _subscribedTopics.Count > 0;

        public uint ExpiryInterval => _connectPacket.SessionExpiryInterval;
        
        public string Id => _connectPacket.ClientId;

        /// <summary>
        ///     Session should persist if CleanSession was set to false (Mqtt3) or if SessionExpiryInterval != 0 (Mqtt5)
        /// </summary>
        public bool IsPersistent { get; set; }

        public IDictionary Items { get; }

        public MqttConnectPacket LatestConnectPacket { get; set; }

        public MqttPacketIdentifierProvider PacketIdentifierProvider { get; } = new MqttPacketIdentifierProvider();

        public long PendingDataPacketsCount => _packetBus.PartitionItemsCount(MqttPacketBusPartition.Data);

        public bool WillMessageSent { get; set; }

        public MqttPublishPacket AcknowledgePublishPacket(ushort packetIdentifier)
        {
            MqttPublishPacket publishPacket;

            lock (_unacknowledgedPublishPackets)
            {
                publishPacket = _unacknowledgedPublishPackets.FirstOrDefault(p => p.PacketIdentifier.Equals(packetIdentifier));
                _unacknowledgedPublishPackets.Remove(publishPacket);
            }

            return publishPacket;
        }

        public void AddSubscribedTopic(string topic)
        {
            if (_subscribedTopics == null)
            {
                _subscribedTopics = new HashSet<string>();
            }

            _subscribedTopics.Add(topic);
        }

        public Task DeleteAsync()
        {
            return _clientSessionsManager.DeleteSessionAsync(Id);
        }

        public Task<MqttPacketBusItem> DequeuePacketAsync(CancellationToken cancellationToken)
        {
            return _packetBus.DequeueItemAsync(cancellationToken);
        }

        public void Dispose()
        {
            _packetBus.Dispose();
            _subscriptionsManager.Dispose();
        }

        public void EnqueueControlPacket(MqttPacketBusItem packetBusItem)
        {
            _packetBus.EnqueueItem(packetBusItem, MqttPacketBusPartition.Control);
        }

        public void EnqueueDataPacket(MqttPacketBusItem packetBusItem)
        {
            if (_packetBus.ItemsCount(MqttPacketBusPartition.Data) >= _serverOptions.MaxPendingMessagesPerClient)
            {
                if (_serverOptions.PendingMessagesOverflowStrategy == MqttPendingMessagesOverflowStrategy.DropNewMessage)
                {
                    return;
                }

                if (_serverOptions.PendingMessagesOverflowStrategy == MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage)
                {
                    // Only drop from the data partition. Dropping from control partition might break the connection
                    // because the client does not receive PINGREQ packets etc. any longer.
                    _packetBus.DropFirstItem(MqttPacketBusPartition.Data);
                }
            }

            var publishPacket = (MqttPublishPacket)packetBusItem.Packet;

            if (publishPacket.QualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce)
            {
                publishPacket.PacketIdentifier = _packetIdentifierProvider.GetNextPacketIdentifier();

                lock (_unacknowledgedPublishPackets)
                {
                    _unacknowledgedPublishPackets.Add(publishPacket);
                }
            }

            _packetBus.EnqueueItem(packetBusItem, MqttPacketBusPartition.Data);
        }

        public void EnqueueHealthPacket(MqttPacketBusItem packetBusItem)
        {
            _packetBus.EnqueueItem(packetBusItem, MqttPacketBusPartition.Health);
        }

        public MqttPublishPacket PeekAcknowledgePublishPacket(ushort packetIdentifier)
        {
            // This will only return the matching PUBLISH packet but does not remove it.
            // This is required for QoS 2.
            lock (_unacknowledgedPublishPackets)
            {
                return _unacknowledgedPublishPackets.FirstOrDefault(p => p.PacketIdentifier.Equals(packetIdentifier));
            }
        }

        public void Recover()
        {
            // TODO: Keep the bus and only insert pending items again.
            // TODO: Check if packet identifier must be restarted or not.
            // TODO: Recover package identifier.
            
            /*
                The Session state in the Client consists of:
                ·         QoS 1 and QoS 2 messages which have been sent to the Server, but have not been completely acknowledged.
                ·         QoS 2 messages which have been received from the Server, but have not been completely acknowledged. 

                The Session state in the Server consists of:
                ·         The existence of a Session, even if the rest of the Session state is empty.
                ·         The Client’s subscriptions.
                ·         QoS 1 and QoS 2 messages which have been sent to the Client, but have not been completely acknowledged.
                ·         QoS 1 and QoS 2 messages pending transmission to the Client.
                ·         QoS 2 messages which have been received from the Client, but have not been completely acknowledged.
                ·         Optionally, QoS 0 messages pending transmission to the Client. 
             */

            // Create a copy of all currently unacknowledged publish packets and clear the storage.
            // We must re-enqueue them in order to trigger other code.
            List<MqttPublishPacket> unacknowledgedPublishPackets;
            lock (_unacknowledgedPublishPackets)
            {
                unacknowledgedPublishPackets = _unacknowledgedPublishPackets.ToList();
                _unacknowledgedPublishPackets.Clear();    
            }

            _packetBus.Clear();

            foreach (var publishPacket in unacknowledgedPublishPackets)
            {
                EnqueueDataPacket(new MqttPacketBusItem(publishPacket));
            }
        }

        public void RemoveSubscribedTopic(string topic)
        {
            _subscribedTopics?.Remove(topic);
        }

        public Task<SubscribeResult> Subscribe(MqttSubscribePacket subscribePacket, CancellationToken cancellationToken)
        {
            return _subscriptionsManager.Subscribe(subscribePacket, cancellationToken);
        }

        public bool TryCheckSubscriptions(string topic, ulong topicHash, MqttQualityOfServiceLevel qualityOfServiceLevel, string senderId, out CheckSubscriptionsResult result)
        {
            result = null;

            try
            {
                result = _subscriptionsManager.CheckSubscriptions(topic, topicHash, qualityOfServiceLevel, senderId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public Task<UnsubscribeResult> Unsubscribe(MqttUnsubscribePacket unsubscribePacket, CancellationToken cancellationToken)
        {
            return _subscriptionsManager.Unsubscribe(unsubscribePacket, cancellationToken);
        }
    }
}