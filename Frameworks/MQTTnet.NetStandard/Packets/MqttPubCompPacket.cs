namespace MQTTnet.Packets
{
    public sealed class MqttPubCompPacket : MqttBasePublishPacket
    {
        public override string ToString()
        {
            return "PubComp";
        }
    }
}
