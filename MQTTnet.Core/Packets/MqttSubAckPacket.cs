using System.Collections.Generic;
using System.Linq;
using MQTTnet.Core.Protocol;

namespace MQTTnet.Core.Packets
{
    public sealed class MqttSubAckPacket : MqttBasePacket, IPacketWithIdentifier
    {
        public ushort PacketIdentifier { get; set; }

        public IList<MqttSubscribeReturnCode> SubscribeReturnCodes { get; set; } = new List<MqttSubscribeReturnCode>();

        public override string ToString()
        {
            var subscribeReturnCodesText = string.Join(",", SubscribeReturnCodes.Select(f => f.ToString()));
            return
                $"{nameof(MqttSubAckPacket)} [PacketIdentifier={PacketIdentifier}] [SubscribeReturnCodes={subscribeReturnCodesText}]";
        }
    }
}
