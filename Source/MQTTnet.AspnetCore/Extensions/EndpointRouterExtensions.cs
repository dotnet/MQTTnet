
#if NETCOREAPP3_1

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace MQTTnet.AspNetCore
{
    public static class EndpointRouterExtensions
    {
        public static void MapMqtt(this IEndpointRouteBuilder endpoints, string pattern) 
        {
            endpoints.MapConnectionHandler<MqttConnectionHandler>(pattern, options =>
            {
                options.WebSockets.SubProtocolSelector = MqttSubProtocolSelector.SelectSubProtocol;
            });
        }
    }
}

#endif
