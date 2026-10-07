// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Extensions.DelayedPublish;

/// <summary>
///     Represents a message that has been accepted by the broker for delayed dispatch.
/// </summary>
public sealed class DelayedMessage
{
    public DelayedMessage(
        Guid id,
        DateTimeOffset dueUtc,
        DateTimeOffset receivedUtc,
        string senderClientId,
        string senderUserName,
        MqttApplicationMessage message)
    {
        Id = id;
        DueUtc = dueUtc;
        ReceivedUtc = receivedUtc;
        SenderClientId = senderClientId;
        SenderUserName = senderUserName;
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }

    /// <summary>
    ///     Unique identifier for the pending message. Stable across persistence boundaries.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    ///     Absolute UTC time at which the message must be dispatched.
    /// </summary>
    public DateTimeOffset DueUtc { get; }

    /// <summary>
    ///     UTC time at which the broker accepted the publish.
    /// </summary>
    public DateTimeOffset ReceivedUtc { get; }

    /// <summary>
    ///     Client identifier of the original publisher (may be <c>null</c> for injected messages).
    /// </summary>
    public string SenderClientId { get; }

    /// <summary>
    ///     User name of the original publisher (may be <c>null</c>).
    /// </summary>
    public string SenderUserName { get; }

    /// <summary>
    ///     The application message to be dispatched, with the real topic already resolved
    ///     (i.e. the <c>$delayed/&lt;interval&gt;/</c> prefix stripped).
    /// </summary>
    public MqttApplicationMessage Message { get; }
}
