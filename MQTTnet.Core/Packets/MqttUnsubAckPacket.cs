namespace MQTTnet.Core.Packets
{
    public sealed class MqttUnsubAckPacket : MqttBasePacket, IPacketWithIdentifier
    {
        public ushort PacketIdentifier { get; set; }
    }
}
