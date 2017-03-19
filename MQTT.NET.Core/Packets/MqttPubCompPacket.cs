namespace MQTTnet.Core.Packets
{
    public class MqttPubCompPacket : MqttBasePacket
    {
        public ushort PacketIdentifier { get; set; }
    }
}
