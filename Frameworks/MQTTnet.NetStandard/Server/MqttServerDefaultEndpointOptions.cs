using System.Net;

namespace MQTTnet.Server
{
    public sealed class MqttServerDefaultEndpointOptions
    {
        public bool IsEnabled { get; set; } = true;

        public int? Port { get; set; }

        public IPAddress BoundIPAddress { get; set; } = IPAddress.Any;
    }
}
