using System;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.AspNetCore;
using MQTTnet.Server.Logging;
using MQTTnet.Server.Mqtt;
using MQTTnet.Server.Scripting;
using MQTTnet.Server.Scripting.DataSharing;

namespace MQTTnet.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }
        
        public IConfigurationRoot Configuration { get; }

        public void Configure(
            IApplicationBuilder application, 
            IHostingEnvironment environment, 
            MqttServerService mqttServerService,
            PythonScriptHostService pythonScriptHostService,
            DataSharingService dataSharingService,
            MqttWebSocketServerAdapter mqttWebSocketServerAdapter)
        {
            if (environment.IsDevelopment())
            {
                application.UseDeveloperExceptionPage();
            }
            else
            {
                application.UseHsts();
            }

            application.UseStaticFiles();

            var webSocketOptions = new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120),
                ReceiveBufferSize = 4 * 1024
            };

            application.UseWebSockets(webSocketOptions);

            application.UseHttpsRedirection();
            application.UseMvc();

            application.Use(async (context, next) =>
            {
                if (context.Request.Path == "/mqtt") // TODO: Full path to config.
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        using (var webSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false))
                        {
                            await mqttWebSocketServerAdapter.RunWebSocketConnectionAsync(webSocket,
                                $"{context.Connection.RemoteIpAddress}:{context.Connection.RemotePort}").ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next().ConfigureAwait(false);
                }
            });

            dataSharingService.Configure();
            pythonScriptHostService.Configure();
            mqttServerService.Configure();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            // Read settings
            var settings = new Configuration.SettingsModel();
            Configuration.Bind("MQTT", settings);
            services.AddSingleton(settings);

            // Wire up dependencies
            services.AddSingleton<PythonIOStream>();
            services.AddSingleton<PythonScriptHostService>();
            services.AddSingleton<DataSharingService>();

            services.AddSingleton<MqttNetLoggerWrapper>();
            services.AddSingleton<MqttWebSocketServerAdapter>();
            services.AddSingleton<CustomMqttFactory>();
            services.AddSingleton<MqttServerService>();

            services.AddSingleton<MqttClientConnectedHandler>();
            services.AddSingleton<MqttClientDisconnectedHandler>();
            services.AddSingleton<MqttClientSubscribedTopicHandler>();
            services.AddSingleton<MqttClientUnsubscribedTopicHandler>();
            services.AddSingleton<MqttConnectionValidator>();
            services.AddSingleton<MqttSubscriptionInterceptor>();
            services.AddSingleton<MqttApplicationMessageInterceptor>();
        }
    }
}