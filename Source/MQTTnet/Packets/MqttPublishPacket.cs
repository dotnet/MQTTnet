using System.Collections.Generic;
using MQTTnet.Packets.Properties;
using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    public class MqttPublishPacket : MqttBasePublishPacket
    {
        public bool Retain { get; set; }

        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }

        public bool Dup { get; set; }

        public string Topic { get; set; }

        public byte[] Payload { get; set; }

        /// <summary>
        /// Added in MQTTv5.0.0.
        /// </summary>
        public List<IProperty> Properties { get; set; }

        public override string ToString()
        {
            return "Publish: [Topic=" + Topic + "]" +
                " [Payload.Length=" + Payload?.Length + "]" +
                " [QoSLevel=" + QualityOfServiceLevel + "]" +
                " [Dup=" + Dup + "]" +
                " [Retain=" + Retain + "]" +
                " [PacketIdentifier=" + PacketIdentifier + "]";
        }
    }
}
