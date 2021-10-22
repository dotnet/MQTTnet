using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public sealed class MqttPubRelPacket : MqttBasePacket, IMqttPacketWithIdentifier
    {
        public ushort PacketIdentifier { get; set; }

        /// <summary>
        /// Added in MQTTv5.
        /// </summary>
        public MqttPubRelReasonCode ReasonCode { get; set; } = MqttPubRelReasonCode.Success;

        /// <summary>
        /// Added in MQTTv5.
        /// </summary>
        public MqttPubRelPacketProperties Properties { get; } = new MqttPubRelPacketProperties();

        public override string ToString()
        {
            return string.Concat("PubRel: [PacketIdentifier=", PacketIdentifier, "] [ReasonCode=", ReasonCode, "]");
        }
    }
}
