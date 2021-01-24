namespace MQTTnet.Diagnostics.PacketInspection
{
    public interface IMqttPacketInspector
    {
        void ProcessMqttPacket(ProcessMqttPacketContext context);
    }
}