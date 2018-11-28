namespace MQTTnet.Packets
{
    public class MqttUnsubAckPacket : MqttBasePacket, IMqttPacketWithIdentifier
    {
        public ushort? PacketIdentifier { get; set; }

        public override string ToString()
        {
            return string.Concat("UnsubAck: [PacketIdentifier=", PacketIdentifier, "]");
        }
    }
}
