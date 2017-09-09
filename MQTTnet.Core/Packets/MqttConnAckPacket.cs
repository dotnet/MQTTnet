using MQTTnet.Core.Protocol;

namespace MQTTnet.Core.Packets
{
    public sealed class MqttConnAckPacket : MqttBasePacket
    {
        public bool IsSessionPresent { get; set; }

        public MqttConnectReturnCode ConnectReturnCode { get; set; }

        public override string ToString()
        {
            return nameof(MqttConnAckPacket) + ": [ConnectReturnCode=" + ConnectReturnCode + "] [IsSessionPresent=" + IsSessionPresent + "]";
        }
    }
}
