using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public sealed class MqttPubRecPacket : MqttBasePacket, IMqttPacketWithIdentifier
    {
        public ushort PacketIdentifier { get; set; }

        #region Added in MQTTv5

        public MqttPubRecReasonCode? ReasonCode { get; set; }

        public MqttPubRecPacketProperties Properties { get; set; }

        #endregion

        public override string ToString()
        {
            return string.Concat("PubRec: [PacketIdentifier=", PacketIdentifier, "] [ReasonCode=", ReasonCode, "]");
        }
    }
}
