using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public sealed class MqttPubRecPacket : MqttBasePacket, IMqttPacketWithIdentifier
    {
        public ushort PacketIdentifier { get; set; }

        /// <summary>
        /// Added in MQTTv5.
        /// </summary>
        public MqttPubRecReasonCode ReasonCode { get; set; } = MqttPubRecReasonCode.Success;

        /// <summary>
        /// Added in MQTTv5.
        /// </summary>
        public MqttPubRecPacketProperties Properties { get; } = new MqttPubRecPacketProperties();

        public override string ToString()
        {
            return string.Concat("PubRec: [PacketIdentifier=", PacketIdentifier, "] [ReasonCode=", ReasonCode, "]");
        }
    }
}
