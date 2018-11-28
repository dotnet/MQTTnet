using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public class MqttPubCompPacket : MqttBasePublishPacket
    {
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
