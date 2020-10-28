using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public sealed class MqttPublishPacket : MqttBasePacket, IMqttPacketWithIdentifier
    {
        public ushort PacketIdentifier { get; set; }

        public bool Retain { get; set; }

        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }

        public bool Dup { get; set; }

        public string Topic { get; set; }

        public byte[] Payload { get; set; }

        #region Added in MQTTv5

        public MqttPublishPacketProperties Properties { get; set; }

        #endregion

        public override string ToString()
        {
            return string.Concat("Publish: [Topic=", Topic, "] [Payload.Length=", Payload?.Length, "] [QoSLevel=", QualityOfServiceLevel, "] [Dup=", Dup, "] [Retain=", Retain, "] [PacketIdentifier=", PacketIdentifier, "]");
        }
    }
}
