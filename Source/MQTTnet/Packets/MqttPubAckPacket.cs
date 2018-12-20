using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public class MqttPubAckPacket : MqttBasePublishPacket
    {
        #region Added in MQTTv5

        public MqttPubAckReasonCode? ReasonCode { get; set; }

        public MqttPubAckPacketProperties Properties { get; set; }

        #endregion

        public override string ToString()
        {
            return string.Concat("PubAck: [PacketIdentifier=", PacketIdentifier, "] [ReasonCode=", ReasonCode, "]");
        }
    }
}
