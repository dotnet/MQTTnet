using System.Collections.Generic;
using MQTTnet.Core.Protocol;

namespace MQTTnet.Core.Packets
{
    public class MqttSubAckPacket : MqttBasePacket
    {
        public ushort PacketIdentifier { get; set; }

        public List<MqttSubscribeReturnCode> SubscribeReturnCodes { get; set; } = new List<MqttSubscribeReturnCode>();
    }
}
