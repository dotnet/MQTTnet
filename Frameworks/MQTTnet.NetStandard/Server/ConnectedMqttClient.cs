using System;
using MQTTnet.Serializer;

namespace MQTTnet.Server
{
    // TODO: Rename to "RegisteredClient"
    // TODO: Add IsConnected
    // TODO: Add interface

    public class ConnectedMqttClient
    {
        public string ClientId { get; set; }

        public MqttProtocolVersion? ProtocolVersion { get; set; }

        public TimeSpan LastPacketReceived { get; set; }

        public TimeSpan LastNonKeepAlivePacketReceived { get; set; }

        public int PendingApplicationMessages { get; set; }
    }
}
