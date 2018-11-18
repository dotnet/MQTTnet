using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public class MqttConnAckPacket : MqttBasePacket
    {
        /// <summary>
        /// Added in MQTTv3.1.1.
        /// </summary>
        public bool IsSessionPresent { get; set; }

        public MqttConnectReturnCode ConnectReturnCode { get; set; }

        public override string ToString()
        {
            return "ConnAck: [ConnectReturnCode=" + ConnectReturnCode + "] [IsSessionPresent=" + IsSessionPresent + "]";
        }
    }
}
