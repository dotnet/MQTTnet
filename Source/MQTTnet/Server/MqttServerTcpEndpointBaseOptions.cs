using System.Net;

namespace MQTTnet.Server
{
    public abstract class MqttServerTcpEndpointBaseOptions
    {
        public bool IsEnabled { get; set; }

        public int Port { get; set; }

        public int ConnectionBacklog { get; set; } = 10;

        public bool NoDelay { get; set; } = true;

#if WINDOWS_UWP
        public int BufferSize { get; set; } = 4096;
#endif

        public IPAddress BoundInterNetworkAddress { get; set; } = IPAddress.Any;

        public IPAddress BoundInterNetworkV6Address { get; set; } = IPAddress.IPv6Any;

        /// <summary>
        /// This requires admin permissions on Linux.
        /// </summary>
        public bool ReuseAddress { get; set; }
    }
}