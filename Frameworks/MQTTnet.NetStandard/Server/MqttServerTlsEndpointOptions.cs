using System.Net;

namespace MQTTnet.Server
{
    public sealed class MqttServerTlsEndpointOptions
    {
        public bool IsEnabled { get; set; }

        public int? Port { get; set; }

        public byte[] Certificate { get; set; }

        public IPAddress BoundIPAddress { get; set; } = IPAddress.Any;
    }
}
