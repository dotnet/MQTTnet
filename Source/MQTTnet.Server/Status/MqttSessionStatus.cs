// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Server.Internal;
using MQTTnet.Server.Internal.Formatter;

namespace MQTTnet.Server;

public sealed class MqttSessionStatus
{
    readonly MqttSession _session;

    public MqttSessionStatus(MqttSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public DateTime CreatedTimestamp => _session.CreatedTimestamp;

    public DateTime? DisconnectedTimestamp => _session.DisconnectedTimestamp;

    public uint ExpiryInterval => _session.ExpiryInterval;

    public string Id => _session.Id;

    public IDictionary Items => _session.Items;

    public long PendingApplicationMessagesCount => _session.PendingDataPacketsCount;

    public Task ClearApplicationMessagesQueueAsync()
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync()
    {
        return _session.DeleteAsync();
    }

    /// <summary>
    /// Delivers an application message immediately to the session.
    /// </summary>
    /// <param name="applicationMessage">The application message to deliver.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The result contains the delivered MQTT publish packet.
    /// </returns>
    public async Task<MqttPublishPacket> DeliverApplicationMessageAsync(MqttApplicationMessage applicationMessage)
    {
        ArgumentNullException.ThrowIfNull(applicationMessage);

        var packetBusItem = new MqttPacketBusItem(MqttPublishPacketFactory.Create(applicationMessage));
        _session.EnqueueDataPacket(packetBusItem);

        var mqttPacket = await packetBusItem.WaitAsync();

        return (MqttPublishPacket)mqttPacket;
    }

    /// <summary>
    /// Attempts to enqueue an application message to the session's send buffer.
    /// </summary>
    /// <param name="applicationMessage">The application message to enqueue.</param>
    /// <param name="publishPacket">The corresponding MQTT publish packed, if the operation was successful.</param>
    /// <returns><c>true</c> if the message was successfully enqueued; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// When <see cref="MqttServerOptions.PendingMessagesOverflowStrategy"/> is set to <see cref="MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage"/>,
    /// this method always returns <c>true</c>.
    /// However, an existing message in the queue may be <b>dropped later</b> to make room for the newly enqueued message.
    /// Such dropped messages can be tracked by subscribing to <see cref="MqttServer.QueuedApplicationMessageOverwrittenAsync"/> event.
    /// </remarks>
    public bool TryEnqueueApplicationMessage(MqttApplicationMessage applicationMessage, out MqttPublishPacket publishPacket)
    {
        ArgumentNullException.ThrowIfNull(applicationMessage);

        publishPacket = MqttPublishPacketFactory.Create(applicationMessage);
        var enqueueDataPacketResult = _session.EnqueueDataPacket(new MqttPacketBusItem(publishPacket));

        if (enqueueDataPacketResult == EnqueueDataPacketResult.Enqueued)
        {
            return true;
        }

        publishPacket = null;
        return false;
    }

    [Obsolete("This method is obsolete. Use TryEnqueueApplicationMessage instead.")]
    public Task EnqueueApplicationMessageAsync(MqttApplicationMessage applicationMessage)
    {
        TryEnqueueApplicationMessage(applicationMessage, out _);
        return CompletedTask.Instance;
    }
}