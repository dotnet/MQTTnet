// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#if NETCOREAPP3_1 || NET5_0_OR_GREATER

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
