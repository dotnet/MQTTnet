namespace MQTTnet.Core.Packets
{
    public sealed class MqttPingReqPacket : MqttBasePacket
    {
        public override string ToString()
        {
            return nameof(MqttPingReqPacket);
        }
    }
}
