// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;

namespace MQTTnet.Extensions.DelayedPublish;

/// <summary>
///     In-memory, non-durable <see cref="IDelayedMessageStore"/>. Pending messages are lost
///     when the process stops.
/// </summary>
public sealed class InMemoryDelayedMessageStore : IDelayedMessageStore
{
    readonly ConcurrentDictionary<Guid, DelayedMessage> _messages = new();

    public ValueTask AddAsync(DelayedMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        _messages[message.Id] = message;
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        _messages.TryRemove(id, out _);
        return ValueTask.CompletedTask;
    }

    public ValueTask<IReadOnlyList<DelayedMessage>> LoadAllAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<DelayedMessage> snapshot = _messages.Values.ToArray();
        return ValueTask.FromResult(snapshot);
    }
}
