// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server.Exceptions;

namespace MQTTnet.Server.Internal;

public sealed class MqttSession : IDisposable
{
    readonly MqttClientSessionsManager _clientSessionsManager;
    readonly MqttConnectPacket _connectPacket;
    readonly object _dataEnqueueLock = new();
    readonly MqttServerEventContainer _eventContainer;
    readonly MqttPacketBus _packetBus = new();
    readonly MqttPacketIdentifierProvider _packetIdentifierProvider = new();
    readonly MqttServerOptions _serverOptions;
    readonly MqttClientSubscriptionsManager _subscriptionsManager;

    // Do not use a dictionary in order to keep the ordering of the messages.
    readonly List<MqttPublishPacket> _unacknowledgedPublishPackets = new();

    // Bookkeeping to know if this is a subscribing client; lazy initialize later.
    HashSet<string> _subscribedTopics;
    bool _disposed;

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
        _eventContainer = eventContainer ?? throw new ArgumentNullException(nameof(eventContainer));

        _subscriptionsManager = new MqttClientSubscriptionsManager(this, eventContainer, retainedMessagesManager, clientSessionsManager);
    }

    public DateTime CreatedTimestamp { get; } = DateTime.UtcNow;

    public DateTime? DisconnectedTimestamp { get; set; }

    public uint ExpiryInterval => _connectPacket.SessionExpiryInterval;

    public bool HasSubscribedTopics => _subscribedTopics != null && _subscribedTopics.Count > 0;

    public string Id => _connectPacket.ClientId;

    public string UserName => _connectPacket.Username;

    public IDictionary Items { get; }

    public MqttConnectPacket LatestConnectPacket { get; set; }

    public MqttPacketIdentifierProvider PacketIdentifierProvider { get; } = new();

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
        lock (_dataEnqueueLock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _packetBus.Dispose();
        }

        _subscriptionsManager.Dispose();
    }

    public void EnqueueControlPacket(MqttPacketBusItem packetBusItem)
    {
        _packetBus.EnqueueItem(packetBusItem, MqttPacketBusPartition.Control);
    }

    public EnqueueDataPacketResult EnqueueDataPacket(MqttPacketBusItem packetBusItem)
    {
        return EnqueueDataPacket(packetBusItem, true, out _);
    }

    internal EnqueueDataPacketResult EnqueueDataPacket(MqttPacketBusItem packetBusItem, bool allowEviction, out ushort packetIdentifier)
    {
        ArgumentNullException.ThrowIfNull(packetBusItem);
        var publishPacket = (MqttPublishPacket)packetBusItem.Packet;
        MqttPacketBusItem overwritten;
        EnqueueDataPacketResult result;
        lock (_dataEnqueueLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            result = EnqueueDataPacketCore(packetBusItem, publishPacket, allowEviction, out overwritten);
            packetIdentifier = result == EnqueueDataPacketResult.Enqueued ? publishPacket.PacketIdentifier : (ushort)0;
        }

        NotifyOverwrittenPacket(overwritten);
        return result;
    }

    // All data producers and recovery share the admission gate. Dequeue may only free capacity.
    EnqueueDataPacketResult EnqueueDataPacketCore(
        MqttPacketBusItem packetBusItem, MqttPublishPacket publishPacket, bool allowEviction, out MqttPacketBusItem overwritten)
    {
        overwritten = null;
        if (PendingDataPacketsCount >= _serverOptions.MaxPendingMessagesPerClient)
        {
            if (!allowEviction)
            {
                // Rejection is backpressure: no packet identifier, tracking entry or faulted promise.
                return EnqueueDataPacketResult.Dropped;
            }

            if (_serverOptions.PendingMessagesOverflowStrategy == MqttPendingMessagesOverflowStrategy.DropNewMessage)
            {
                packetBusItem.Fail(new MqttPendingMessagesOverflowException(Id, _serverOptions.PendingMessagesOverflowStrategy));
                return EnqueueDataPacketResult.Dropped;
            }

            if (_serverOptions.PendingMessagesOverflowStrategy == MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage)
            {
                // Only drop from the data partition. Dropping from control partition might break the connection
                // because the client does not receive PINGREQ packets etc. any longer.
                overwritten = _packetBus.DropFirstItem(MqttPacketBusPartition.Data);
                if (overwritten != null)
                {
                    overwritten.Fail(new MqttPendingMessagesOverflowException(Id, _serverOptions.PendingMessagesOverflowStrategy));
                }
            }
        }

        if (publishPacket.QualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce)
        {
            publishPacket.PacketIdentifier = _packetIdentifierProvider.GetNextPacketIdentifier();

            lock (_unacknowledgedPublishPackets)
            {
                _unacknowledgedPublishPackets.Add(publishPacket);
            }
        }

        _packetBus.EnqueueItem(packetBusItem, MqttPacketBusPartition.Data);
        return EnqueueDataPacketResult.Enqueued;
    }

    void NotifyOverwrittenPacket(MqttPacketBusItem overwritten)
    {
        if (overwritten != null && _eventContainer.QueuedApplicationMessageOverwrittenEvent.HasHandlers)
        {
            var eventArgs = new QueueMessageOverwrittenEventArgs(Id, overwritten.Packet);
            _eventContainer.QueuedApplicationMessageOverwrittenEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
        }
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
        List<MqttPacketBusItem> overwrittenPackets = null;
        lock (_dataEnqueueLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            List<MqttPublishPacket> unacknowledgedPublishPackets;
            lock (_unacknowledgedPublishPackets)
            {
                unacknowledgedPublishPackets = _unacknowledgedPublishPackets.ToList();
                _unacknowledgedPublishPackets.Clear();
            }

            _packetBus.Clear();

            foreach (var publishPacket in unacknowledgedPublishPackets)
            {
                EnqueueDataPacketCore(new MqttPacketBusItem(publishPacket), publishPacket, true, out var overwritten);
                if (overwritten != null)
                {
                    (overwrittenPackets ??= new List<MqttPacketBusItem>()).Add(overwritten);
                }
            }
        }

        if (overwrittenPackets != null)
        {
            foreach (var overwritten in overwrittenPackets)
            {
                NotifyOverwrittenPacket(overwritten);
            }
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
