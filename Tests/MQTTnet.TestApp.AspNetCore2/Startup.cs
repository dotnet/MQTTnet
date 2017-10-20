using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Server;

namespace MQTTnet.TestApp.AspNetCore2
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public async void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(LogLevel.Debug);

            MqttNetTrace.TraceMessagePublished += (s, e) =>
            {
                Debug.WriteLine($">> [{e.TraceMessage.Timestamp}] [{e.TraceMessage.ThreadId}] [{e.TraceMessage.Source}] [{e.TraceMessage.Level}]: {e.TraceMessage.Message}");
                if (e.TraceMessage.Exception != null)
                {
                    Debug.WriteLine(e.TraceMessage.Exception.Message);
                }
            };

            var trace = new MqttNetTrace();
            var adapter = new MqttWebSocketServerAdapter(trace);
            var options = new MqttServerOptions();
            var mqttServer = new MqttServer(options, new List<IMqttServerAdapter> { adapter }, new MqttNetTrace());
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
