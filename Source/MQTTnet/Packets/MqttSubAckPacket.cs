using System.Collections.Generic;
using System.Linq;
using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public sealed class MqttSubAckPacket : MqttBasePacket, IMqttPacketWithIdentifier
    {
        public ushort PacketIdentifier { get; set; }

        /// <summary>
        /// Only available in MQTT v3.1.1.
        /// </summary>
        public List<MqttSubscribeReturnCode> ReturnCodes { get; } = new List<MqttSubscribeReturnCode>();
        
        /// <summary>
        /// Added in MQTTv5.
        /// </summary>
        public List<MqttSubscribeReasonCode> ReasonCodes { get; } = new List<MqttSubscribeReasonCode>();

        /// <summary>
        /// Added in MQTTv5.
        /// </summary>
        public MqttSubAckPacketProperties Properties { get; } = new MqttSubAckPacketProperties();

        public override string ToString()
        {
            var returnCodesText = string.Join(",", ReturnCodes.Select(f => f.ToString()));
            var reasonCodesText = string.Join(",", ReasonCodes.Select(f => f.ToString()));

            return string.Concat("SubAck: [PacketIdentifier=", PacketIdentifier, "] [ReturnCodes=", returnCodesText, "] [ReasonCode=", reasonCodesText, "]");
        }
    }
}
