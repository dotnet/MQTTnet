using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Server;

namespace MQTTnet.AspNetCore
{
    public static class ApplicationBuilderExtensions
    {
        [Obsolete("This class is obsolete and will be removed in a future version. The recommended alternative is to use MapMqtt inside Microsoft.AspNetCore.Builder.UseEndpoints(...).")]
        public static IApplicationBuilder UseMqttEndpoint(this IApplicationBuilder app, string path = "/mqtt")
        {
            app.UseWebSockets();
            app.Use(async (context, next) =>
            {
                if (!context.WebSockets.IsWebSocketRequest || context.Request.Path != path)
                {
                    await next();
                    return;
                }

                string subProtocol = null;

                if (context.Request.Headers.TryGetValue("Sec-WebSocket-Protocol", out var requestedSubProtocolValues))
                {
                    subProtocol = MqttSubProtocolSelector.SelectSubProtocol(requestedSubProtocolValues);
                }

                var adapter = app.ApplicationServices.GetRequiredService<MqttWebSocketServerAdapter>();
                using (var webSocket = await context.WebSockets.AcceptWebSocketAsync(subProtocol).ConfigureAwait(false))
                {
                    await adapter.RunWebSocketConnectionAsync(webSocket, context);
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
