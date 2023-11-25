using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Extensions.Hosting.Implementations;
using MQTTnet.Extensions.Hosting.Options;
using MQTTnet.Implementations;
using MQTTnet.Server;

namespace MQTTnet.Extensions.Hosting.Extensions
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder UseMqttServer(this IHostBuilder hostBuilder)
        {
            if (hostBuilder == null)
            {
                throw new ArgumentNullException(nameof(hostBuilder));
            }

            return hostBuilder.UseMqttServer(
                builder =>
                {
                    builder.WithDefaultEndpoint();
                });
        }

        public static IHostBuilder UseMqttServer(this IHostBuilder hostBuilder, Action<MqttServerHostingBuilder> configure)
        {
            if (hostBuilder == null)
            {
                throw new ArgumentNullException(nameof(hostBuilder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var startActions = new List<Action<MqttServer>>();
            var stopActions = new List<Action<MqttServer>>();

            hostBuilder.ConfigureServices(
                (_, services) =>
                {
                    services.AddSingleton(
                        s =>
                        {
                            var builder = new MqttServerHostingBuilder(s, startActions, stopActions);
                            configure(builder);
                            return builder.Build();
                        });

                    var logger = new MqttNetEventLogger();

                    services.AddSingleton<IMqttNetLogger>(logger)
                        .AddSingleton<MqttHostedServer>()
                        .AddSingleton<IMqttNetLogger>(new MqttNetNullLogger())
                        .AddSingleton(new MqttFactory())
                        .AddSingleton<MqttServerHostingOptions>()
                        .AddSingleton<IHostedService>(s => s.GetRequiredService<MqttHostedServer>())
                        .AddSingleton<IHostedService>(s => new MqttServerConfigurationHostedService(s, startActions, stopActions))
                        .AddSingleton<MqttServer>(s => s.GetRequiredService<MqttHostedServer>())
                        .AddSingleton<MqttTcpServerAdapter>()
                        .AddSingleton<IMqttServerAdapter>(s => s.GetRequiredService<MqttTcpServerAdapter>())
                        .AddSingleton<MqttServerWebSocketConnectionHandler>()
                        .AddSingleton<MqttWebSocketServerAdapter>()
                        .AddSingleton<IMqttServerAdapter>(s => s.GetRequiredService<MqttWebSocketServerAdapter>());
                });

            return hostBuilder;
        }
    }
}