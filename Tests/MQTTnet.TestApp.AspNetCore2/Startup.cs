using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Server;

namespace MQTTnet.TestApp.AspNetCore2
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMqttServer();
            services.AddSingleton<MqttWebSocketServerAdapter>();
            services.AddSingleton<IMqttServerAdapter, MqttWebSocketServerAdapter>();
        }

        public async void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(LogLevel.Debug);
            loggerFactory.AddDebug();

            var adapter = app.ApplicationServices.GetService<MqttWebSocketServerAdapter>();
            var mqttServer = app.ApplicationServices.GetService<IMqttServer>();
            await mqttServer.StartAsync();
            
            app.UseWebSockets();
            app.Use(async (context, next) =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    using (var webSocket = await context.WebSockets.AcceptWebSocketAsync())
                    {
                        await adapter.AcceptWebSocketAsync(webSocket);
                    }
                }
                else
                {
                    await next();
                }
            });
        }
    }
}
