using System.Net;

namespace MQTTnet.Server
{
    public abstract class MqttServerTcpEndpointBaseOptions
    {
        public bool IsEnabled { get; set; }

        public int Port { get; set; }

        public int ConnectionBacklog { get; set; } = 10;

#if WINDOWS_UWP
        public int BufferSize { get; set; } = 4096;
#endif

        public IPAddress BoundInterNetworkAddress { get; set; } = IPAddress.Any;

        public IPAddress BoundInterNetworkV6Address { get; set; } = IPAddress.IPv6Any;
    }
}