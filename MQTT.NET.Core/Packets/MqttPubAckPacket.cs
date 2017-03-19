namespace MQTTnet.Core.Packets
{
    public class MqttPubAckPacket : MqttBasePacket
    {
        public ushort PacketIdentifier { get; set; }
    }
}
