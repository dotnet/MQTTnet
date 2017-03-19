using MQTTnet.Core.Protocol;

namespace MQTTnet.Core.Packets
{
    public class MqttPublishPacket : MqttBasePacket
    {
        public ushort? PacketIdentifier { get; set; }

        public bool Retain { get; set; }

        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }

        public bool Dup { get; set; }

        public string Topic { get; set; }

        public byte[] Payload { get; set; }
    }
}
