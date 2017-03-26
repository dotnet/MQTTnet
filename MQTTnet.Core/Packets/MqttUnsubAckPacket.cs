namespace MQTTnet.Core.Packets
{
    public class MqttUnsubAckPacket : MqttBasePacket, IPacketWithIdentifier
    {
        public ushort PacketIdentifier { get; set; }
    }
}
