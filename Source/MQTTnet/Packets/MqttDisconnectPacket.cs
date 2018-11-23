namespace MQTTnet.Packets
{
    public class MqttDisconnectPacket : MqttBasePacket
    {
        public override string ToString()
        {
            return "Disconnect";
        }
    }
}
