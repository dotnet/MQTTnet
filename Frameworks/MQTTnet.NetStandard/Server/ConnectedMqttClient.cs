using System;
using MQTTnet.Serializer;

namespace MQTTnet.Server
{
    public class ConnectedMqttClient
    {
        public string ClientId { get; set; }

        public MqttProtocolVersion ProtocolVersion { get; set; }

        public TimeSpan LastPacketReceived { get; set; }

        public TimeSpan LastNonKeepAlivePacketReceived { get; set; }
    }
}
