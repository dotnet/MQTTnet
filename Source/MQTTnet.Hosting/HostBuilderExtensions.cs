using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Hosting;
using MQTTnet.Implementations;
using MQTTnet.Server;
using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Hosting
{
    public static class HostBuilderExtensions
    {

        public static IHostBuilder UseMqttServer(this IHostBuilder hostBuilder)
            => UseMqttServer(hostBuilder, builder =>
            {
                builder.WithDefaultEndpoint();
            });


        public static IHostBuilder UseMqttServer(this IHostBuilder hostBuilder, Action<MqttServerHostingBuilder> configure)
        {
            var configureActions = new List<Action<MqttServer>>();
            hostBuilder.ConfigureServices((context, services) =>
            {
                services.AddSingleton(s =>
                {
                    var builder = new MqttServerHostingBuilder(s, configureActions);
                    configure(builder);
                    return builder.Build();
                });

                var logger = new MqttNetEventLogger();

                services.AddSingleton<IMqttNetLogger>(logger);
                services.AddSingleton<MqttHostedServer>();
                services.AddSingleton<IHostedService>(s => s.GetRequiredService<MqttHostedServer>());
                services.AddSingleton<IHostedService>(s => new MqttServerConfigurationHostedService(s, configureActions));
                services.AddSingleton<MqttServer>(s => s.GetRequiredService<MqttHostedServer>());

                services.AddSingleton<MqttTcpServerAdapter>();
                services.AddSingleton<IMqttServerAdapter>(s => s.GetService<MqttTcpServerAdapter>());

            });
            return hostBuilder;
        }

    }
}
