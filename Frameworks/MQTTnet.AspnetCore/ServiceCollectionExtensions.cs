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
        public static IServiceCollection AddHostedMqttServer(this IServiceCollection services, Action<AspNetMqttServerOptionsBuilder> configureOptions=null)
        {
            var builder = new AspNetMqttServerOptionsBuilder();

            configureOptions?.Invoke(builder);

            var options = builder.Build();

            services.AddSingleton(options);
            services.AddSingleton<IMqttServerOptions>(options);
            services.AddSingleton<IMqttNetLogger>(new MqttNetLogger());
            services.AddSingleton<MqttHostedServer>();
            services.AddSingleton<IHostedService>(s => s.GetService<MqttHostedServer>());
            services.AddSingleton<IMqttServer>(s => s.GetService<MqttHostedServer>());
            
            services.AddSingleton<MqttWebSocketServerAdapter>();            
            services.AddSingleton<IMqttServerAdapter>(s => s.GetService<MqttWebSocketServerAdapter>());

            if (options.ListenTcp)
            {
                services.AddSingleton<MqttTcpServerAdapter>();
                services.AddSingleton<IMqttServerAdapter>(s => s.GetService<MqttTcpServerAdapter>());
            }           

            return services;
        }

        
    }
}
