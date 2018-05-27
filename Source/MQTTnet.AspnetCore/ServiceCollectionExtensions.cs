using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Server;
using MQTTnet.Implementations;

namespace MQTTnet.AspNetCore
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHostedMqttServer(this IServiceCollection services, IMqttServerOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            var logger = new MqttNetLogger();
            var childLogger = logger.CreateChildLogger();

            services.AddSingleton(options);
            services.AddSingleton<IMqttNetLogger>(logger);
            services.AddSingleton(childLogger);
            services.AddSingleton<MqttHostedServer>();
            services.AddSingleton<IHostedService>(s => s.GetService<MqttHostedServer>());
            services.AddSingleton<IMqttServer>(s => s.GetService<MqttHostedServer>());

            services.AddSingleton<MqttWebSocketServerAdapter>();
            services.AddSingleton<MqttTcpServerAdapter>();
            services.AddSingleton<IMqttServerAdapter>(s => s.GetService<MqttWebSocketServerAdapter>());

            if (options.DefaultEndpointOptions.IsEnabled)
            {
                services.AddSingleton<MqttConnectionHandler>();
                services.AddSingleton<IMqttServerAdapter>(s => s.GetService<MqttConnectionHandler>());
            }

            return services;
        }
    }
}
