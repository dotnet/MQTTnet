namespace MQTTnet.Core.Packets
{
    public class MqttBasePublishPacket : MqttBasePacket, IPacketWithIdentifier
    {
        public ushort PacketIdentifier { get; set; }
    }
}
