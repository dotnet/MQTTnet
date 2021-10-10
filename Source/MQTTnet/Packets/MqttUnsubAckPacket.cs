using System.Collections.Generic;
using System.Linq;
using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public sealed class MqttUnsubAckPacket : MqttBasePacket, IMqttPacketWithIdentifier
    {
        public ushort PacketIdentifier { get; set; }

        /// <summary>
        /// Added in MQTT V5.
        /// </summary>
        public MqttUnsubAckPacketProperties Properties { get; set; } = new MqttUnsubAckPacketProperties();

        /// <summary>
        /// Added in MQTT V5.
        /// </summary>
        public List<MqttUnsubscribeReasonCode> ReasonCodes { get; set; } = new List<MqttUnsubscribeReasonCode>();

        public override string ToString()
        {
            var reasonCodesText = string.Join(",", ReasonCodes.Select(f => f.ToString()));

            return string.Concat("UnsubAck: [PacketIdentifier=", PacketIdentifier, "] [ReasonCodes=", reasonCodesText, "]");
        }
    }
}
