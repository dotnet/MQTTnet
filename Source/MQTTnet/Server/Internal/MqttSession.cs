// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Concurrent;
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

        readonly MqttServerOptions _serverOptions;
        readonly MqttClientSubscriptionsManager _subscriptionsManager;

        readonly ConcurrentDictionary<ushort, MqttPublishPacket> _unacknowledgedPublishPackets = new ConcurrentDictionary<ushort, MqttPublishPacket>();

        // Bookkeeping to know if this is a subscribing client; lazy initialize later.
        HashSet<string> _subscribedTopics;

        public MqttSession(
            string clientId,
            bool isPersistent,
            IDictionary items,
            MqttServerOptions serverOptions,
            MqttServerEventContainer eventContainer,
            MqttRetainedMessagesManager retainedMessagesManager,
            MqttClientSessionsManager clientSessionsManager)
        {
            Id = clientId ?? throw new ArgumentNullException(nameof(clientId));
            IsPersistent = isPersistent;
            Items = items ?? throw new ArgumentNullException(nameof(items));

            _serverOptions = serverOptions ?? throw new ArgumentNullException(nameof(serverOptions));
            _clientSessionsManager = clientSessionsManager ?? throw new ArgumentNullException(nameof(clientSessionsManager));

            _subscriptionsManager = new MqttClientSubscriptionsManager(this, eventContainer, retainedMessagesManager, clientSessionsManager);
        }

        public DateTime CreatedTimestamp { get; } = DateTime.UtcNow;

        public bool HasSubscribedTopics => _subscribedTopics != null && _subscribedTopics.Count > 0;

        public string Id { get; }

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
            _unacknowledgedPublishPackets.TryRemove(packetIdentifier, out var publishPacket);
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

                _unacknowledgedPublishPackets[publishPacket.PacketIdentifier] = publishPacket;
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
            _unacknowledgedPublishPackets.TryGetValue(packetIdentifier, out var publishPacket);
            return publishPacket;
        }

        public void Recover()
        {
            // TODO: Keep the bus and only insert pending items again.
            // TODO: Check if packet identifier must be restarted or not.
            // TODO: Recover package identifier.
            _packetBus.Clear();

            foreach (var publishPacket in _unacknowledgedPublishPackets.Values.ToList())
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