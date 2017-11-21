namespace MQTTnet.Core.Packets
{
    public sealed class MqttPubRecPacket : MqttBasePublishPacket
    {
        public override string ToString()
        {
            return "PubRec";
        }
    }
}
