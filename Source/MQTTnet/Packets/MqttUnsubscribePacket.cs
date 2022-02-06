// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace MQTTnet.Packets
{
    public sealed class MqttUnsubscribePacket : MqttBasePacket, IMqttPacketWithIdentifier
    {
        public ushort PacketIdentifier { get; set; }

        public List<string> TopicFilters { get; } = new List<string>();

        /// <summary>
        /// Added in MQTTv5.
        /// </summary>
        public MqttUnsubscribePacketProperties Properties { get; } = new MqttUnsubscribePacketProperties();
        
        public override string ToString()
        {
            var topicFiltersText = string.Join(",", TopicFilters);
            return string.Concat("Unsubscribe: [PacketIdentifier=", PacketIdentifier, "] [TopicFilters=", topicFiltersText, "]");
        }
    }
}
