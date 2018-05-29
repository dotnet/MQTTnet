namespace MQTTnet.Packets
{
    public class MqttBasePublishPacket : MqttBasePacket, IMqttPacketWithIdentifier
    {
        public ushort? PacketIdentifier { get; set; }
    }
}
