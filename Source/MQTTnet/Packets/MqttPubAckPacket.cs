using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public class MqttPubAckPacket : MqttBasePublishPacket
    {
        #region Added in MQTTv5

        public MqttPubAckReasonCode? ConnectReasonCode { get; set; }

        public MqttPubAckPacketProperties Properties { get; set; }

        #endregion

        public override string ToString()
        {
            return $"PubAck: [PacketIdentifier={PacketIdentifier}] [ConnectReasonCode={ConnectReasonCode}]";
        }
    }
}
