// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Extensions.DelayedPublish;

/// <summary>
///     Options controlling the behavior of the EMQX-compatible delayed publish feature.
/// </summary>
public sealed class DelayedPublishOptions
{
    /// <summary>
    ///     The topic prefix that triggers delayed publish handling. Defaults to <c>$delayed</c>
    ///     to match the EMQX convention (<c>$delayed/&lt;Interval&gt;/&lt;RealTopic&gt;</c>).
    /// </summary>
    public string TopicPrefix { get; set; } = "$delayed";

    /// <summary>
    ///     Maximum accepted delay. Publishes requesting a longer delay are rejected.
    ///     Defaults to 30 days.
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    ///     Maximum number of messages that may be pending dispatch at any time.
    ///     Additional publishes are rejected with <see cref="Protocol.MqttPubAckReasonCode.QuotaExceeded"/>.
    /// </summary>
    public int MaxPendingMessages { get; set; } = 100_000;

    /// <summary>
    ///     Threshold (in seconds) below which the interval value is interpreted as a relative delay
    ///     and above which it is interpreted as an absolute Unix epoch timestamp.
    ///     The default (4 294 967) matches the EMQX behavior (largest value that fits in a 32-bit
    ///     unsigned integer of milliseconds).
    /// </summary>
    public long AbsoluteTimeThresholdSeconds { get; set; } = 4_294_967;

    /// <summary>
    ///     When <c>true</c> (default) the original sender's client identifier, user name and
    ///     session items are preserved when the message is dispatched.
    /// </summary>
    public bool PreserveSenderIdentity { get; set; } = true;

    /// <summary>
    ///     Optional authorization callback invoked before a delayed message is accepted.
    ///     Returning <c>false</c> rejects the publish.
    /// </summary>
    public Func<DelayedMessage, CancellationToken, ValueTask<bool>> AuthorizeAsync { get; set; }
}
