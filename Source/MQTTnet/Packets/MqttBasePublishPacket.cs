using System.Collections.Generic;
using MQTTnet.Packets.Properties;

namespace MQTTnet.Packets
{
    public class MqttBasePublishPacket : MqttBasePacket, IMqttPacketWithIdentifier
    {
        public ushort? PacketIdentifier { get; set; }

        /// <summary>
        /// Added in MQTTv5.0.0.
        /// </summary>
        public List<IProperty> Properties { get; set; }
    }
}
