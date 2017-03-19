using System.Collections.Generic;

namespace MQTTnet.Core.Packets
{
    public class MqttSubscribePacket : MqttBasePacket
    {
        public ushort PacketIdentifier { get; set; }
        
        public IList<TopicFilter> TopicFilters { get; set; } = new List<TopicFilter>();
    }
}
