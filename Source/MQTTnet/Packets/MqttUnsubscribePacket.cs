using System.Collections.Generic;

namespace MQTTnet.Packets
{
    public sealed class MqttUnsubscribePacket : MqttBasePacket, IMqttPacketWithIdentifier
    {
        public ushort PacketIdentifier { get; set; }

        public List<string> TopicFilters { get; set; } = new List<string>();

        #region Added in MQTTv5

        public MqttUnsubscribePacketProperties Properties { get; set; }

        #endregion

        public override string ToString()
        {
            var topicFiltersText = string.Join(",", TopicFilters);
            return string.Concat("Unsubscribe: [PacketIdentifier=", PacketIdentifier, "] [TopicFilters=", topicFiltersText, "]");
        }
    }
}
