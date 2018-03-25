namespace MQTTnet.Packets
{
    public interface IMqttPacketWithIdentifier
    {
        ushort? PacketIdentifier { get; set; }
    }
}
