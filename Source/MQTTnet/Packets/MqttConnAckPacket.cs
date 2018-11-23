using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public class MqttConnAckPacket : MqttBasePacket
    {
        public MqttConnectReturnCode ConnectReturnCode { get; set; }

        /// <summary>
        /// Added in MQTTv3.1.1.
        /// </summary>
        public bool IsSessionPresent { get; set; }

        #region Added in MQTTv5

        public MqttConnectReasonCode? ConnectReasonCode { get; set; }

        public MqttConnAckPacketProperties Properties { get; set; }

        #endregion

        public override string ToString()
        {
            return "ConnAck: [ConnectReturnCode=" + ConnectReturnCode + "] [IsSessionPresent=" + IsSessionPresent + "]";
        }
    }
}
