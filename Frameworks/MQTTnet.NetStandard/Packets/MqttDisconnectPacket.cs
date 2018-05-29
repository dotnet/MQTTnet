namespace MQTTnet.Packets
{
    public sealed class MqttDisconnectPacket : MqttBasePacket
    {
        public override string ToString()
        {
            return "Disconnect";
        }
    }
}
