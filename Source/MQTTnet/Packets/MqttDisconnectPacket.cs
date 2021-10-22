using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public sealed class MqttDisconnectPacket : MqttBasePacket
    {
        /// <summary>
        /// Added in MQTTv5.
        /// </summary>
        public MqttDisconnectReasonCode ReasonCode { get; set; } = MqttDisconnectReasonCode.NormalDisconnection;

        /// <summary>
        /// Added in MQTTv5.
        /// </summary>
        public MqttDisconnectPacketProperties Properties { get; } = new MqttDisconnectPacketProperties();

        public override string ToString()
        {
            return string.Concat("Disconnect: [ReasonCode=", ReasonCode, "]");
        }
    }
}
