using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public sealed class MqttPubAckPacket : MqttBasePacket, IMqttPacketWithIdentifier
    {
        public ushort PacketIdentifier { get; set; }

        /// <summary>
        /// Added in MQTTv5.
        /// </summary>
        public MqttPubAckReasonCode ReasonCode { get; set; } = MqttPubAckReasonCode.Success;

        /// <summary>
        /// Added in MQTTv5.
        /// </summary>
        public MqttPubAckPacketProperties Properties { get; } = new MqttPubAckPacketProperties();

        public override string ToString()
        {
            return string.Concat("PubAck: [PacketIdentifier=", PacketIdentifier, "] [ReasonCode=", ReasonCode, "]");
        }
    }
}
