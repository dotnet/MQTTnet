// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Routing;
using MQTTnet.AspNetCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Builder
{
    public static class EndpointRouteBuilderExtensions
    {
        /// <summary>
        /// mqtt over websocket
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public static ConnectionEndpointRouteBuilder MapMqtt(this IEndpointRouteBuilder endpoints, string pattern)
        {
            ArgumentNullException.ThrowIfNull(endpoints);
            return endpoints.MapMqtt(pattern, options => options.WebSockets.SubProtocolSelector = SelectSubProtocol);

            static string SelectSubProtocol(IList<string> requestedSubProtocolValues)
            {
                // Order the protocols to also match "mqtt", "mqttv-3.1", "mqttv-3.11" etc.
                return requestedSubProtocolValues.OrderByDescending(p => p.Length).FirstOrDefault(p => p.ToLower().StartsWith("mqtt"));
            }
        }

        /// <summary>
        /// mqtt over websocket
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="pattern"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static ConnectionEndpointRouteBuilder MapMqtt(this IEndpointRouteBuilder endpoints, string pattern, Action<HttpConnectionDispatcherOptions> options)
        {
            ArgumentNullException.ThrowIfNull(endpoints);
            return endpoints.MapConnectionHandler<MqttConnectionHandler>(pattern, options);
        }
    }
}

