using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public sealed class MqttConnAckPacket : MqttBasePacket
    {
        public MqttConnectReturnCode ReturnCode { get; set; }
        
        /// <summary>
        /// Added in MQTT 3.1.1.
        /// </summary>
        public bool IsSessionPresent { get; set; }
        
        /// <summary>
        /// Added in MQTT 5.0.0.
        /// </summary>
        public MqttConnectReasonCode ReasonCode { get; set; }

        /// <summary>
        /// Added in MQTT 5.0.0.
        /// </summary>
        public MqttConnAckPacketProperties Properties { get; } = new MqttConnAckPacketProperties();
        
        public override string ToString()
        {
            return string.Concat("ConnAck: [ReturnCode=", ReturnCode, "] [ReasonCode=", ReasonCode, "] [IsSessionPresent=", IsSessionPresent, "]");
        }
    }
}
