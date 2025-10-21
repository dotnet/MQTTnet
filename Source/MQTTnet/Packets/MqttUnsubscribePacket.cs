// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Packets;

public sealed class MqttUnsubscribePacket : MqttPacketWithIdentifier
{
    public List<string> TopicFilters { get; set; } = [];

    /// <summary>
    ///     Added in MQTTv5.
    /// </summary>
    public List<MqttUserProperty> UserProperties { get; set; }

    public override string ToString()
    {
        var topicFiltersText = string.Join(",", TopicFilters);
        return $"Unsubscribe: [PacketIdentifier={PacketIdentifier}] [TopicFilters={topicFiltersText}]";
    }
}