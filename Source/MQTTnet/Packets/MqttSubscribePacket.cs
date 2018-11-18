using System.Collections.Generic;
using System.Linq;
using MQTTnet.Packets.Properties;

namespace MQTTnet.Packets
{
    public class MqttSubscribePacket : MqttBasePacket, IMqttPacketWithIdentifier
    {
        public ushort? PacketIdentifier { get; set; }

        public IList<TopicFilter> TopicFilters { get; set; } = new List<TopicFilter>();

        /// <summary>
        /// Added in MQTTv5.0.0.
        /// </summary>
        public List<IProperty> Properties { get; set; }

        public override string ToString()
        {
            var topicFiltersText = string.Join(",", TopicFilters.Select(f => f.Topic + "@" + f.QualityOfServiceLevel));
            return "Subscribe: [PacketIdentifier=" + PacketIdentifier + "] [TopicFilters=" + topicFiltersText + "]";
        }
    }
}
