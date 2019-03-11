using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Scripting.Utils;
using MQTTnet.Server.Configuration;
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
            SettingsModel settings)
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

            application.UseHttpsRedirection();
            application.UseMvc();

            ConfigureWebSocketEndpoint(application, mqttServerService, settings);

            dataSharingService.Configure();
            pythonScriptHostService.Configure();

            mqttServerService.Configure();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddSingleton(ReadSettings());

            services.AddSingleton<PythonIOStream>();
            services.AddSingleton<PythonScriptHostService>();
            services.AddSingleton<DataSharingService>();

            services.AddSingleton<MqttNetLoggerWrapper>();
            services.AddSingleton<CustomMqttFactory>();
            services.AddSingleton<MqttServerService>();
            services.AddSingleton<MqttServerStorage>();

            services.AddSingleton<MqttClientConnectedHandler>();
            services.AddSingleton<MqttClientDisconnectedHandler>();
            services.AddSingleton<MqttClientSubscribedTopicHandler>();
            services.AddSingleton<MqttClientUnsubscribedTopicHandler>();
            services.AddSingleton<MqttConnectionValidator>();
            services.AddSingleton<MqttSubscriptionInterceptor>();
            services.AddSingleton<MqttApplicationMessageInterceptor>();
        }

        private SettingsModel ReadSettings()
        {
            var settings = new Configuration.SettingsModel();
            Configuration.Bind("MQTT", settings);
            return settings;
        }

        private static void ConfigureWebSocketEndpoint(
            IApplicationBuilder application,
            MqttServerService mqttServerService,
            SettingsModel settings)
        {
            if (settings?.WebSocketEndPoint?.Enabled != true)
            {
                return;
            }

            if (string.IsNullOrEmpty(settings.WebSocketEndPoint.Path))
            {
                return;
            }

            var webSocketOptions = new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(settings.WebSocketEndPoint.KeepAliveInterval),
                ReceiveBufferSize = settings.WebSocketEndPoint.ReceiveBufferSize
            };

            if (settings.WebSocketEndPoint.AllowedOrigins?.Any() == true)
            {
                webSocketOptions.AllowedOrigins.AddRange(settings.WebSocketEndPoint.AllowedOrigins);
            }
            
            application.UseWebSockets(webSocketOptions);

            application.Use(async (context, next) =>
            {
                if (context.Request.Path == settings.WebSocketEndPoint.Path)
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        using (var webSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false))
                        {
                            await mqttServerService.RunWebSocketConnectionAsync(webSocket, context).ConfigureAwait(false);
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
        }
    }
}