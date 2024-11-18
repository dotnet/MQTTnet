// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MQTTnet.AspNetCore
{
    public static class ConnectionBuilderExtensions
    {
        /// <summary>
        /// Treat the obtained connection as an mqtt connection
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="protocols"></param>
        /// <returns></returns>
        public static IConnectionBuilder UseMqtt(this IConnectionBuilder builder, MqttProtocols protocols = MqttProtocols.MqttAndHttp)
        {
            builder.ApplicationServices.GetRequiredService<MqttConnectionHandler>().UseFlag = true;
            if (protocols == MqttProtocols.Mqtt)
            {
                return builder.UseConnectionHandler<MqttConnectionHandler>();
            }
            else if (protocols == MqttProtocols.MqttAndHttp)
            {
                var middleware = builder.ApplicationServices.GetRequiredService<MqttConnectionMiddleware>();
                return builder.Use(next => context => middleware.InvokeAsync(next, context));
            }

            throw new NotSupportedException(protocols.ToString());
        }
    }
}
