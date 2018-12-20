using System.Collections.Generic;
using System.Linq;

namespace MQTTnet.Packets
{
    public class MqttSubscribePacket : MqttBasePacket, IMqttPacketWithIdentifier
    {
        public ushort? PacketIdentifier { get; set; }

        public List<TopicFilter> TopicFilters { get; } = new List<TopicFilter>();

        #region Added in MQTTv5

        public MqttSubscribePacketProperties Properties { get; set; }

        #endregion

        public override string ToString()
        {
            var topicFiltersText = string.Join(",", TopicFilters.Select(f => f.Topic + "@" + f.QualityOfServiceLevel));
            return string.Concat("Subscribe: [PacketIdentifier=", PacketIdentifier, "] [TopicFilters=", topicFiltersText, "]");
        }
    }
}
