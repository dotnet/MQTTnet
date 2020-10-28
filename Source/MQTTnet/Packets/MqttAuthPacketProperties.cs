using System.Collections.Generic;

namespace MQTTnet.Packets
{
    public sealed class MqttAuthPacketProperties
    {
        public string AuthenticationMethod { get; set; }

        public byte[] AuthenticationData { get; set; }

        public string ReasonString { get; set; }

        public List<MqttUserProperty> UserProperties { get; set; }
    }
}
