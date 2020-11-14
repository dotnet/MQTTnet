using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public sealed class MqttConnAckPacket : MqttBasePacket
    {
        public MqttConnectReturnCode? ReturnCode { get; set; }

        #region Added in MQTTv3.1.1

        public bool IsSessionPresent { get; set; }

        #endregion

        #region Added in MQTTv5.0.0

        public MqttConnectReasonCode? ReasonCode { get; set; }

        public MqttConnAckPacketProperties Properties { get; set; }

        #endregion

        public override string ToString()
        {
            return string.Concat("ConnAck: [ReturnCode=", ReturnCode, "] [ReasonCode=", ReasonCode, "] [IsSessionPresent=", IsSessionPresent, "]");
        }
    }
}
