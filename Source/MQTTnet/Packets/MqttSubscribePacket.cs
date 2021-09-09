﻿using System.Collections.Generic;
using System.Linq;

namespace MQTTnet.Packets
{
    public sealed class MqttSubscribePacket : MqttBasePacket, IMqttPacketWithIdentifier
    {
        public ushort PacketIdentifier { get; set; }

        public List<MqttTopicFilter> TopicFilters { get; set; } = new List<MqttTopicFilter>();

        /// <summary>
        /// Added in MQTT V5.
        /// </summary>
        public MqttSubscribePacketProperties Properties { get; set; } = new MqttSubscribePacketProperties();
        
        public override string ToString()
        {
            var topicFiltersText = string.Join(",", TopicFilters.Select(f => f.Topic + "@" + f.QualityOfServiceLevel));
            return string.Concat("Subscribe: [PacketIdentifier=", PacketIdentifier, "] [TopicFilters=", topicFiltersText, "]");
        }
    }
}
