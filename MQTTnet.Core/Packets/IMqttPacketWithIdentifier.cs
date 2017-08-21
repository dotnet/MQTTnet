namespace MQTTnet.Core.Packets
{
    public interface IMqttPacketWithIdentifier
    {
        ushort PacketIdentifier { get; set; }
    }
}
