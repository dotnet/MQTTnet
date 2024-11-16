// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MQTTnet.AspNetCore
{
    public static class EndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Treat the obtained WebSocket as an mqtt connection
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static ConnectionEndpointRouteBuilder MapMqtt(this IEndpointRouteBuilder endpoints, string pattern)
        {
            return endpoints.MapMqtt(pattern, options => options.WebSockets.SubProtocolSelector = SelectSubProtocol);

            static string SelectSubProtocol(IList<string> requestedSubProtocolValues)
            {
                // Order the protocols to also match "mqtt", "mqttv-3.1", "mqttv-3.11" etc.
                return requestedSubProtocolValues.OrderByDescending(p => p.Length).FirstOrDefault(p => p.ToLower().StartsWith("mqtt"))!;
            }
        }

        /// <summary>
        /// Treat the obtained WebSocket as an mqtt connection
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="pattern"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static ConnectionEndpointRouteBuilder MapMqtt(this IEndpointRouteBuilder endpoints, string pattern, Action<HttpConnectionDispatcherOptions> options)
        {
            endpoints.ServiceProvider.GetRequiredService<MqttConnectionHandler>().MapFlag = true;
            return endpoints.MapConnectionHandler<MqttConnectionHandler>(pattern, options);
        }
    }
}

