using System.Collections.Generic;

namespace MQTTnet.Packets
{
    public sealed class MqttUnsubscribePacket : MqttBasePacket, IMqttPacketWithIdentifier
    {
        public ushort PacketIdentifier { get; set; }

        public List<string> TopicFilters { get; set; } = new List<string>();

        /// <summary>
        /// Added in MQTTv5.
        /// </summary>
        public MqttUnsubscribePacketProperties Properties { get; set; } = new MqttUnsubscribePacketProperties();
        
        public override string ToString()
        {
            var topicFiltersText = string.Join(",", TopicFilters);
            return string.Concat("Unsubscribe: [PacketIdentifier=", PacketIdentifier, "] [TopicFilters=", topicFiltersText, "]");
        }
    }
}
