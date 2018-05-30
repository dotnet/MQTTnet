namespace MQTTnet.Packets
{
    public class MqttUnsubAckPacket : MqttBasePacket, IMqttPacketWithIdentifier
    {
        public ushort? PacketIdentifier { get; set; }

        public override string ToString()
        {
            return "UnsubAck: [PacketIdentifier=" + PacketIdentifier + "]";
        }
    }
}
