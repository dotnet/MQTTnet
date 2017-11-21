namespace MQTTnet.Core.Packets
{
    public sealed class MqttPubAckPacket : MqttBasePublishPacket
    {
        public override string ToString()
        {
            return "PubAck";
        }
    }
}
