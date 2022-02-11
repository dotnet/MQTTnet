// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

namespace MQTTnet.Packets
{
    public sealed class MqttSubscribePacket : MqttBasePacket, IMqttPacketWithIdentifier
    {
        public ushort PacketIdentifier { get; set; }

        /// <summary>
        ///     It is a Protocol Error if the Subscription Identifier has a value of 0.
        /// </summary>
        public uint SubscriptionIdentifier { get; set; }

        public List<MqttTopicFilter> TopicFilters { get; set; } = new List<MqttTopicFilter>();

        /// <summary>
        ///     Added in MQTTv5.
        /// </summary>
        public List<MqttUserProperty> UserProperties { get; set; }

        public override string ToString()
        {
            var topicFiltersText = string.Join(",", TopicFilters.Select(f => f.Topic + "@" + f.QualityOfServiceLevel));
            return $"Subscribe: [PacketIdentifier={PacketIdentifier}] [TopicFilters={topicFiltersText}]";
        }
    }
}