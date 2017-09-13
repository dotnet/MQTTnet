using System;
using MQTTnet.Core.Protocol;

namespace MQTTnet.Core.Packets
{
    public sealed class MqttPublishPacket : MqttBasePublishPacket
    {
        public bool Retain { get; set; }

        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }

        public bool Dup { get; set; }

        public string Topic { get; set; }

        public byte[] Payload { get; set; }

        public override string ToString()
        {
            return nameof(MqttPublishPacket) +
                ": [Topic=" + Topic + "]" +
                " [Payload=" + Convert.ToBase64String(Payload) + "]" +
                " [QoSLevel=" + QualityOfServiceLevel + "]" +
                " [Dup=" + Dup + "]" +
                " [Retain=" + Retain + "]" +
                " [PacketIdentifier=" + PacketIdentifier + "]";
        }
    }
}
