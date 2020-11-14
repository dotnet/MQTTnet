using System.Collections.Generic;
using System.Linq;
using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public sealed class MqttUnsubAckPacket : MqttBasePacket, IMqttPacketWithIdentifier
    {
        public ushort PacketIdentifier { get; set; }

        #region Added in MQTTv5

        public MqttUnsubAckPacketProperties Properties { get; set; }

        public List<MqttUnsubscribeReasonCode> ReasonCodes { get; set; } = new List<MqttUnsubscribeReasonCode>();

        #endregion

        public override string ToString()
        {
            var reasonCodesText = string.Join(",", ReasonCodes.Select(f => f.ToString()));

            return string.Concat("UnsubAck: [PacketIdentifier=", PacketIdentifier, "] [ReasonCodes=", reasonCodesText, "]");
        }
    }
}
