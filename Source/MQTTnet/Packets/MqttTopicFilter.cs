// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Protocol;

namespace MQTTnet.Packets;

public sealed class MqttTopicFilter
{
    /// <summary>
    ///     Gets or sets a value indicating whether the sender will not receive its own published application messages.
    ///     <remarks>MQTT 5.0.0+ feature.</remarks>
    /// </summary>
    public bool NoLocal { get; set; }

    /// <summary>
    ///     Gets or sets the quality of service level.
    ///     The Quality of Service (QoS) level is an agreement between the sender of a message and the receiver of a message
    ///     that defines the guarantee of delivery for a specific message.
    ///     There are 3 QoS levels in MQTT:
    ///     - At most once  (0): Message gets delivered no time, once or multiple times.
    ///     - At least once (1): Message gets delivered at least once (one time or more often).
    ///     - Exactly once  (2): Message gets delivered exactly once (It's ensured that the message only comes once).
    /// </summary>
    public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether messages are retained as published or not.
    ///     <remarks>MQTT 5.0.0+ feature.</remarks>
    /// </summary>
    public bool RetainAsPublished { get; set; }

    /// <summary>
    ///     Gets or sets the retain handling.
    ///     <remarks>MQTT 5.0.0+ feature.</remarks>
    /// </summary>
    public MqttRetainHandling RetainHandling { get; set; } = MqttRetainHandling.SendAtSubscribe;

    /// <summary>
    ///     Gets or sets the MQTT topic.
    ///     In MQTT, the word topic refers to an UTF-8 string that the broker uses to filter messages for each connected
    ///     client.
    ///     The topic consists of one or more topic levels. Each topic level is separated by a forward slash (topic level
    ///     separator).
    /// </summary>
    public required string Topic { get; set; }

    public override string ToString()
    {
        return
            $"TopicFilter: [Topic={Topic}] [QualityOfServiceLevel={QualityOfServiceLevel}] [NoLocal={NoLocal}] [RetainAsPublished={RetainAsPublished}] [RetainHandling={RetainHandling}]";
    }
}