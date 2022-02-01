using MQTTnet.Formatter;

namespace MQTTnet.Adapter
{
    public interface IMqttPacketInspectorHandler
    {
        void BeginReceivePacket();

        void FillReceiveBuffer(byte[] buffer);
        
        void EndReceivePacket();

        void BeginSendPacket(MqttPacketBuffer buffer);
    }
}