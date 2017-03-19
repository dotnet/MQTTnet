namespace MQTTnet.Core.Packets
{
    public class MqttPubRelPacket : MqttBasePacket
    {
        public ushort PacketIdentifier { get; set; }
    }
}
