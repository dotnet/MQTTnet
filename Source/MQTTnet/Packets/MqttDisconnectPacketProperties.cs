using System.Collections.Generic;

namespace MQTTnet.Packets
{
    public sealed class MqttDisconnectPacketProperties
    {
        public uint? SessionExpiryInterval { get; set; }

        public string ReasonString { get; set; }

        public List<MqttUserProperty> UserProperties { get; set; }

        public string ServerReference { get; set; }
    }
}