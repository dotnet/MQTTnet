namespace MQTTnet.Packets
{
    public sealed class MqttPubRecPacket : MqttBasePublishPacket
    {
        public override string ToString()
        {
            return "PubRec";
        }
    }
}
