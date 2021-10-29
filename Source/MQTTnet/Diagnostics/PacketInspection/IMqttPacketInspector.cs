namespace MQTTnet.Diagnostics
{
    public interface IMqttPacketInspector
    {
        void ProcessMqttPacket(ProcessMqttPacketContext context);
    }
}