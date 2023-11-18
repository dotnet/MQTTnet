using System.Net;

namespace MQTTnet.Extensions.Hosting.Options
{
    public abstract class MqttServerWebSocketEndpointBaseOptions
    {

        public bool IsEnabled { get; set; }

        public int Port { get; set; }

        public IPAddress BoundInterNetworkAddress { get; set; } = IPAddress.Any;

        public IPAddress BoundInterNetworkV6Address { get; set; } = IPAddress.IPv6Any;

    }
}
