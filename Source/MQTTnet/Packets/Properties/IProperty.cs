using MQTTnet.Serializer;

namespace MQTTnet.Packets.Properties
{
    public interface IProperty
    {
        byte Id { get; }

        void WriteTo(MqttPacketWriter writer);
    }
}
