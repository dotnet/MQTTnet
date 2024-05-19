// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace MQTTnet.AspNetCore
{
    public static class EndpointRouterExtensions
    {
        public static void MapMqtt(this IEndpointRouteBuilder endpoints, string pattern)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            endpoints.MapConnectionHandler<MqttConnectionHandler>(pattern, options =>
            {
                options.WebSockets.SubProtocolSelector = MqttSubProtocolSelector.SelectSubProtocol;
            });
        }
    }
}

