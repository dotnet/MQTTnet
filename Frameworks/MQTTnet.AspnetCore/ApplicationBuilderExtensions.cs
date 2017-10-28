using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Core.Server;

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
                    var adapter = app.ApplicationServices.GetRequiredService<MqttWebSocketServerAdapter>();
                    using (var webSocket = await context.WebSockets.AcceptWebSocketAsync("mqtt"))
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
