using System.Collections.Generic;
using MQTTnet.Packets.Properties;
using MQTTnet.Protocol;

namespace MQTTnet.Packets
{
    /// <summary>
    /// Added in MQTTv5.0.0.
    /// </summary>
    public class MqttAuthPacket : MqttBasePacket
    {
        public MqttAuthenticateReasonCode ReasonCode { get; set; }

        public List<IProperty> Properties { get; set; }
    }
}
