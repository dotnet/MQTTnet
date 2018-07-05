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
            
            services.AddSingleton(options);

            services.AddHostedMqttServer();

            return services;
        }
        
        public static IServiceCollection AddHostedMqttServer(this IServiceCollection services, Action<MqttServerOptionsBuilder> configure)
        {
            var builder = new MqttServerOptionsBuilder();

            configure(builder);
            
            services.AddSingleton<IMqttServerOptions>(builder.Build());

            services.AddHostedMqttServer();

            return services;
        }

        public static IServiceCollection AddHostedMqttServer<TOptions>(this IServiceCollection services)
            where TOptions : class, IMqttServerOptions
        {
            services.AddSingleton<IMqttServerOptions, TOptions>();

            services.AddHostedMqttServer();

            return services;
        }

        private static IServiceCollection AddHostedMqttServer(this IServiceCollection services)
        {
            var logger = new MqttNetLogger();
            var childLogger = logger.CreateChildLogger();

            services.AddSingleton<IMqttNetLogger>(logger);
            services.AddSingleton(childLogger);
            services.AddSingleton<MqttHostedServer>();
            services.AddSingleton<IHostedService>(s => s.GetService<MqttHostedServer>());
            services.AddSingleton<IMqttServer>(s => s.GetService<MqttHostedServer>());
            
            return services;
        }

        public static IServiceCollection AddMqttWebSocketServerAdapter(this IServiceCollection services)
        {
            services.AddSingleton<MqttWebSocketServerAdapter>();
            services.AddSingleton<IMqttServerAdapter>(s => s.GetService<MqttWebSocketServerAdapter>());

            return services;
        }

        public static IServiceCollection AddMqttTcpServerAdapter(this IServiceCollection services)
        {
            services.AddSingleton<MqttTcpServerAdapter>();
            services.AddSingleton<IMqttServerAdapter>(s => s.GetService<MqttTcpServerAdapter>());

            return services;
        }

        public static IServiceCollection AddMqttConnectionHandler(this IServiceCollection services)
        {
            services.AddSingleton<MqttConnectionHandler>();
            services.AddSingleton<IMqttServerAdapter>(s => s.GetService<MqttConnectionHandler>());

            return services;
        }
    }
}
