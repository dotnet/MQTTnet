namespace MQTTnet.Core.Packets
{
    public class MqttUnsubAckPacket : MqttBasePacket
    {
        public ushort PacketIdentifier { get; set; }
    }
}
