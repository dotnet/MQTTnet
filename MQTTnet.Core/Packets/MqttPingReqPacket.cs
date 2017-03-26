namespace MQTTnet.Core.Packets
{
    public class MqttPingReqPacket : MqttBasePacket
    {
        public override string ToString()
        {
            return nameof(MqttPingReqPacket);
        }
    }
}
