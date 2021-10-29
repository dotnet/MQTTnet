using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Implementations;
using MQTTnet.Server;

namespace MQTTnet.AspNetCore.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHostedMqttServer(this IServiceCollection services, MqttServerOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            services.AddSingleton(options);

            services.AddHostedMqttServer();

            return services;
        }

        public static IServiceCollection AddHostedMqttServer(this IServiceCollection services, Action<MqttServerOptionsBuilder> configure)
        {
            services.AddSingleton<MqttServerOptions>(s =>
            {
                var builder = new MqttServerOptionsBuilder();
                configure(builder);
                return builder.Build();
            });

            services.AddHostedMqttServer();

            return services;
        }

        public static IServiceCollection AddHostedMqttServerWithServices(this IServiceCollection services, Action<AspNetMqttServerOptionsBuilder> configure)
        {
            services.AddSingleton<MqttServerOptions>(s =>
            {
                var builder = new AspNetMqttServerOptionsBuilder(s);
                configure(builder);
                return builder.Build();
            });

            services.AddHostedMqttServer();

            return services;
        }

        // public static IServiceCollection AddHostedMqttServer<TOptions>(this IServiceCollection services)
        //     where TOptions : MqttServerOptions
        // {
        //     services.AddSingleton<MqttServerOptions, TOptions>();
        //
        //     services.AddHostedMqttServer();
        //
        //     return services;
        // }

        static IServiceCollection AddHostedMqttServer(this IServiceCollection services)
        {
            var logger = new MqttNetEventLogger();

            services.AddSingleton<IMqttNetLogger>(logger);
            services.AddSingleton<MqttHostedServer>();
            services.AddSingleton<IHostedService>(s => s.GetService<MqttHostedServer>());
            services.AddSingleton<MqttServer>(s => s.GetService<MqttHostedServer>());

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
