using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Server;

namespace MQTTnet.AspNetCore
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHostedMqttServer(this IServiceCollection services, MqttServerOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            services.AddSingleton(options);
            services.AddSingleton<IMqttNetLogger>(new MqttNetLogger());
            services.AddSingleton<MqttHostedServer>();
            services.AddSingleton<IHostedService>(s => s.GetService<MqttHostedServer>());
            services.AddSingleton<IMqttServer>(s => s.GetService<MqttHostedServer>());
            
            services.AddSingleton<MqttWebSocketServerAdapter>();
            services.AddSingleton<IMqttServerAdapter>(s => s.GetService<MqttWebSocketServerAdapter>());

            return services;
        }
    }
}
