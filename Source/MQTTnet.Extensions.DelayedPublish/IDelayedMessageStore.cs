// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Extensions.DelayedPublish;

/// <summary>
///     Pluggable store for messages awaiting delayed dispatch. The default implementation
///     (<see cref="InMemoryDelayedMessageStore"/>) keeps messages in process memory and does
///     not survive restarts; provide a custom implementation to add durable persistence.
/// </summary>
public interface IDelayedMessageStore
{
    /// <summary>
    ///     Persists a newly accepted delayed message.
    /// </summary>
    ValueTask AddAsync(DelayedMessage message, CancellationToken cancellationToken);

    /// <summary>
    ///     Removes a message after it has been dispatched or discarded.
    /// </summary>
    ValueTask RemoveAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    ///     Loads all currently pending messages. Called once during startup to rehydrate
    ///     the scheduler.
    /// </summary>
    ValueTask<IReadOnlyList<DelayedMessage>> LoadAllAsync(CancellationToken cancellationToken);
}
