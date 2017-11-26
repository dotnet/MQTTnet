using System;
using MQTTnet.Serializer;

namespace MQTTnet.Server
{
    public class ConnectedMqttClient
    {
        public string ClientId { get; set; }

        public MqttProtocolVersion ProtocolVersion { get; set; }

        public TimeSpan LastPacketReceivedDuration { get; set; }

        public TimeSpan LastNonKeepAlivePacketReceivedDuration{ get; set; }
    }
}
