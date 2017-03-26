using System.Text;
using MQTTnet.Core.Protocol;

namespace MQTTnet.Core.Packets
{
    public class MqttPublishPacket : MqttBasePublishPacket
    {
        public bool Retain { get; set; }

        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }

        public bool Dup { get; set; }

        public string Topic { get; set; }

        public byte[] Payload { get; set; }

        public override string ToString()
        {
            return
                $"{nameof(MqttPublishPacket)} [Topic={Topic}] [Payload={Encoding.UTF8.GetString(Payload, 0, Payload.Length)}] [QoSLevel={QualityOfServiceLevel}] [Dup={Dup}] [Retain={Retain}] [PacketIdentifier={PacketIdentifier}]";
        }
    }
}
