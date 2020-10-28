using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public sealed class MqttPubCompPacket : MqttBasePacket, IMqttPacketWithIdentifier
    {
        public ushort PacketIdentifier { get; set; }

        #region Added in MQTTv5

        public MqttPubCompReasonCode? ReasonCode { get; set; }

        public MqttPubCompPacketProperties Properties { get; set; }

        #endregion

        public override string ToString()
        {
            return string.Concat("PubComp: [PacketIdentifier=", PacketIdentifier, "] [ReasonCode=", ReasonCode, "]");
        }
    }
}
