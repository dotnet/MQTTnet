using MQTTnet.Serializer;

namespace MQTTnet.Packets.Properties
{
    public interface IPropertyValue
    {
        void WriteTo(MqttPacketWriter writer);
    }
}
