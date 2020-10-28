using System.Collections.Generic;
using System.Linq;
using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public sealed class MqttSubAckPacket : MqttBasePacket, IMqttPacketWithIdentifier
    {
        public ushort PacketIdentifier { get; set; }

        public List<MqttSubscribeReturnCode> ReturnCodes { get; set; } = new List<MqttSubscribeReturnCode>();

        #region Added in MQTTv5.0.0

        public List<MqttSubscribeReasonCode> ReasonCodes { get; } = new List<MqttSubscribeReasonCode>();

        public MqttSubAckPacketProperties Properties { get; set; }

        #endregion

        public override string ToString()
        {
            var returnCodesText = string.Join(",", ReturnCodes.Select(f => f.ToString()));
            var reasonCodesText = string.Join(",", ReasonCodes.Select(f => f.ToString()));

            return string.Concat("SubAck: [PacketIdentifier=", PacketIdentifier, "] [ReturnCodes=", returnCodesText, "] [ReasonCode=", reasonCodesText, "]");
        }
    }
}
