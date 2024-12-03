// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using MQTTnet.Internal;
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
    /// A task that represents the asynchronous operation.
    /// The result contains the <see cref="InjectMqttApplicationMessageResult"/> that includes the packet identifier of the enqueued message.
    /// </returns>
    public async Task<InjectMqttApplicationMessageResult> DeliverApplicationMessageAsync(MqttApplicationMessage applicationMessage)
    {
        ArgumentNullException.ThrowIfNull(applicationMessage);

        var publishPacket = MqttPublishPacketFactory.Create(applicationMessage);
        var packetBusItem = new MqttPacketBusItem(publishPacket);
        _session.EnqueueDataPacket(packetBusItem);

        await packetBusItem.WaitAsync().ConfigureAwait(false);

        var injectResult = new InjectMqttApplicationMessageResult()
        {
            PacketIdentifier = publishPacket.PacketIdentifier
        };

        return injectResult;
    }

    /// <summary>
    /// Attempts to enqueue an application message to the session's send buffer.
    /// </summary>
    /// <param name="applicationMessage">The application message to enqueue.</param>
    /// <param name="injectResult"><see cref="InjectMqttApplicationMessageResult"/> that includes the packet identifier of the enqueued message.</param>
    /// <returns><c>true</c> if the message was successfully enqueued; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// When <see cref="MqttServerOptions.PendingMessagesOverflowStrategy"/> is set to <see cref="MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage"/>,
    /// this method always returns <c>true</c>.
    /// However, an existing message in the queue may be <b>dropped later</b> to make room for the newly enqueued message.
    /// Such dropped messages can be tracked by subscribing to <see cref="MqttServer.QueuedApplicationMessageOverwrittenAsync"/> event.
    /// </remarks>
    public bool TryEnqueueApplicationMessage(MqttApplicationMessage applicationMessage, out InjectMqttApplicationMessageResult injectResult)
    {
        ArgumentNullException.ThrowIfNull(applicationMessage);

        var publishPacket = MqttPublishPacketFactory.Create(applicationMessage);
        var enqueueDataPacketResult = _session.EnqueueDataPacket(new MqttPacketBusItem(publishPacket));

        if (enqueueDataPacketResult != EnqueueDataPacketResult.Enqueued)
        {
            injectResult = null;
            return false;
        }

        injectResult = new InjectMqttApplicationMessageResult() { PacketIdentifier = publishPacket.PacketIdentifier };
        return true;
    }

    [Obsolete("This method is obsolete. Use TryEnqueueApplicationMessage instead.")]
    public Task EnqueueApplicationMessageAsync(MqttApplicationMessage applicationMessage)
    {
        TryEnqueueApplicationMessage(applicationMessage, out _);
        return CompletedTask.Instance;
    }
}