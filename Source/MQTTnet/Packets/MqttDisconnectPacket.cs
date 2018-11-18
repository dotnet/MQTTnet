using System.Collections.Generic;
using MQTTnet.Packets.Properties;

namespace MQTTnet.Packets
{
    public class MqttDisconnectPacket : MqttBasePacket
    {
        /// <summary>
        /// Added in MQTTv5.0.0.
        /// </summary>
        public List<IProperty> Properties { get; set; }

        public override string ToString()
        {
            return "Disconnect";
        }
    }
}
