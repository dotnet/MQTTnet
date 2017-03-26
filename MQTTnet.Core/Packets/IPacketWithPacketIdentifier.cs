namespace MQTTnet.Core.Packets
{
    public interface IPacketWithIdentifier
    {
        ushort PacketIdentifier { get; set; }
    }
}
