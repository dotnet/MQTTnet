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

        public List<MqttTopicFilter> TopicFilters { get; } = new List<MqttTopicFilter>();

        /// <summary>
        /// Added in MQTT V5.
        /// </summary>
        public MqttSubscribePacketProperties Properties { get; } = new MqttSubscribePacketProperties();
        
        public override string ToString()
        {
            var topicFiltersText = string.Join(",", TopicFilters.Select(f => f.Topic + "@" + f.QualityOfServiceLevel));
            return string.Concat("Subscribe: [PacketIdentifier=", PacketIdentifier, "] [TopicFilters=", topicFiltersText, "]");
        }
    }
}
