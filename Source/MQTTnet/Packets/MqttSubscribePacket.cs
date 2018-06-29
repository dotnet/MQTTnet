using System.Collections.Generic;
using System.Linq;

namespace MQTTnet.Packets
{
    public class MqttSubscribePacket : MqttBasePacket, IMqttPacketWithIdentifier
    {
        public ushort? PacketIdentifier { get; set; }

        public IList<TopicFilter> TopicFilters { get; set; } = new List<TopicFilter>();

        public override string ToString()
        {
            var topicFiltersText = string.Join(",", TopicFilters.Select(f => f.Topic + "@" + f.QualityOfServiceLevel));
            return "Subscribe: [PacketIdentifier=" + PacketIdentifier + "] [TopicFilters=" + topicFiltersText + "]";
        }
    }
}
