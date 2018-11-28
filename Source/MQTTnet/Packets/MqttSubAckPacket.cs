using System.Collections.Generic;
using System.Linq;
using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public class MqttSubAckPacket : MqttBasePacket, IMqttPacketWithIdentifier
    {
        public ushort? PacketIdentifier { get; set; }

        public List<MqttSubscribeReturnCode> SubscribeReturnCodes { get; } = new List<MqttSubscribeReturnCode>();

        #region Added in MQTTv5

        public List<MqttSubscribeReasonCode?> ReasonCodes { get; set; }

        public MqttSubAckPacketProperties Properties { get; set; }

        #endregion

        public override string ToString()
        {
            var subscribeReturnCodesText = string.Join(",", SubscribeReturnCodes.Select(f => f.ToString()));
            return string.Concat("SubAck: [PacketIdentifier=", PacketIdentifier, "] [SubscribeReturnCodes=", subscribeReturnCodesText, "]");
        }
    }
}
