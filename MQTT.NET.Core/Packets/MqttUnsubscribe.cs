using System.Collections.Generic;

namespace MQTTnet.Core.Packets
{
    public class MqttUnsubscribePacket : MqttBasePacket
    {
        public ushort PacketIdentifier { get; set; }
        
        public IList<string> TopicFilters { get; set; } = new List<string>();
    }
}
