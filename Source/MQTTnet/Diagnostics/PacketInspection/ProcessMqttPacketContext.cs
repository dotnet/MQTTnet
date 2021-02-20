namespace MQTTnet.Diagnostics.PacketInspection
{
    public sealed class ProcessMqttPacketContext
    {
        public MqttPacketFlowDirection Direction { get; set; }

        public byte[] Buffer { get; set; }
    }
}