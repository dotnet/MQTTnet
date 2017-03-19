namespace MQTTnet.Core.Packets
{
    public class MqttPubRecPacket : MqttBasePacket
    {
        public ushort PacketIdentifier { get; set; }
    }
}
