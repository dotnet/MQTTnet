using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Microsoft.Scripting.Utils;
using MQTTnet.Server.Configuration;
using MQTTnet.Server.Logging;
using MQTTnet.Server.Mqtt;
using MQTTnet.Server.Scripting;
using MQTTnet.Server.Scripting.DataSharing;
using Newtonsoft.Json.Converters;
using Swashbuckle.AspNetCore.SwaggerUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace MQTTnet.Server.Web
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
            MqttSettingsModel mqttSettings)
        {
            if (environment.IsDevelopment())
            {
                application.UseDeveloperExceptionPage();
            }
            else
            {
                application.UseHsts();
            }

            application.UseCors(x => x
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());

            application.UseAuthentication();

            application.UseStaticFiles();

            application.UseHttpsRedirection();
            application.UseMvc();

            ConfigureWebSocketEndpoint(application, mqttServerService, mqttSettings);

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
            services.AddCors();

            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
                .AddNewtonsoftJson(o =>
                {
                    o.SerializerSettings.Converters.Add(new StringEnumConverter());
                });

            ReadMqttSettings(services);

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
            services.AddSingleton<MqttServerConnectionValidator>();
            services.AddSingleton<MqttSubscriptionInterceptor>();
            services.AddSingleton<MqttApplicationMessageInterceptor>();

            services.AddSwaggerGen(c =>
            {
                c.DescribeAllEnumsAsStrings();

                var securityScheme = new OpenApiSecurityScheme
                {
                    Scheme = "Basic",
                    Name = HeaderNames.Authorization,
                    Type = SecuritySchemeType.Http,
                    In = ParameterLocation.Header
                };

                c.AddSecurityDefinition("Swagger", securityScheme);

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    [securityScheme] = new List<string>()
                });

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

            services.AddAuthentication("Basic")
                .AddScheme<AuthenticationSchemeOptions, AuthenticationHandler>("Basic", null)
                .AddCookie();
        }

        private void ReadMqttSettings(IServiceCollection services)
        {
            var mqttSettings = new MqttSettingsModel();
            Configuration.Bind("MQTT", mqttSettings);
            services.AddSingleton(mqttSettings);

            var scriptingSettings = new ScriptingSettingsModel();
            Configuration.Bind("Scripting", scriptingSettings);
            services.AddSingleton(scriptingSettings);
        }

        private static void ConfigureWebSocketEndpoint(
            IApplicationBuilder application,
            MqttServerService mqttServerService,
            MqttSettingsModel mqttSettings)
        {
            if (mqttSettings?.WebSocketEndPoint?.Enabled != true)
            {
                return;
            }

            if (string.IsNullOrEmpty(mqttSettings.WebSocketEndPoint.Path))
            {
                return;
            }

            var webSocketOptions = new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(mqttSettings.WebSocketEndPoint.KeepAliveInterval),
                ReceiveBufferSize = mqttSettings.WebSocketEndPoint.ReceiveBufferSize
            };

            if (mqttSettings.WebSocketEndPoint.AllowedOrigins?.Any() == true)
            {
                webSocketOptions.AllowedOrigins.AddRange(mqttSettings.WebSocketEndPoint.AllowedOrigins);
            }

            application.UseWebSockets(webSocketOptions);

            application.Use(async (context, next) =>
            {
                if (context.Request.Path == mqttSettings.WebSocketEndPoint.Path)
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
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
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