﻿using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using MQTTnet.Server;

namespace MQTTnet.AspNetCore
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseMqttEndpoint(this IApplicationBuilder app, string path = "/mqtt")
        {
            app.UseWebSockets();
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == path && context.WebSockets.IsWebSocketRequest)
                {
                    string subprotocol = null;

                    if (context.Request.Headers.TryGetValue("Sec-WebSocket-Protocol", out var requestedSubProtocolValues))
                    {
                        // Order the protocols to also match "mqtt", "mqttv-3.1", "mqttv-3.11" etc.
                        subprotocol = requestedSubProtocolValues
                            .OrderByDescending(p => p.Length)
                            .FirstOrDefault(p => p.ToLower().StartsWith("mqtt"));
                    }

                    var adapter = app.ApplicationServices.GetRequiredService<MqttWebSocketServerAdapter>();
                    using (var webSocket = await context.WebSockets.AcceptWebSocketAsync(subprotocol))
                    {
                        await adapter.AcceptWebSocketAsync(webSocket);
                    }
                }
                else
                {
                    await next();
                }
            });

            return app;
        }

        public static IApplicationBuilder UseMqttServer(this IApplicationBuilder app, Action<IMqttServer> configure)
        {
            var server = app.ApplicationServices.GetRequiredService<IMqttServer>();

            configure(server);

            return app;
        }
    }
}
