using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Microsoft.Scripting.Utils;
using MQTTnet.Server.Configuration;
using MQTTnet.Server.Logging;
using MQTTnet.Server.Mqtt;
using MQTTnet.Server.Scripting;
using MQTTnet.Server.Scripting.DataSharing;
using Swashbuckle.AspNetCore.SwaggerUI;

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

            application.UseSwagger(o => o.RouteTemplate = "/api/{documentName}/swagger.json");

            application.UseSwaggerUI(o =>
            {
                o.RoutePrefix = "api";
                o.DocumentTitle = "MQTTnet.Server API";
                o.SwaggerEndpoint("/api/v1/swagger.json", "MQTTnet.Server API v1");
                o.DisplayRequestDuration();
                o.DocExpansion(DocExpansion.List);
                o.DefaultModelRendering(ModelRendering.Model);
            });
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                });

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
            
            services.AddSwaggerGen(c =>
            {
                c.DescribeAllEnumsAsStrings();
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "MQTTnet.Server API",
                    Version = "v1",
                    Description = "The public API for the MQTT broker MQTTnet.Server.",
                    License = new OpenApiLicense
                    {
                        Name = "MIT",
                        Url = new Uri("https://github.com/chkr1011/MQTTnet/blob/master/README.md")
                    },
                    Contact = new OpenApiContact
                    {
                        Name = "MQTTnet.Server",
                        Email = string.Empty,
                        Url = new Uri("https://github.com/chkr1011/MQTTnet")
                    },
                });
            });
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