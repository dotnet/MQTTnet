using System.Collections.Generic;

namespace MQTTnet.Core.Packets
{
    public sealed class MqttUnsubscribePacket : MqttBasePacket, IMqttPacketWithIdentifier
    {
        public ushort PacketIdentifier { get; set; }

        public IList<string> TopicFilters { get; set; } = new List<string>();
    }
}
