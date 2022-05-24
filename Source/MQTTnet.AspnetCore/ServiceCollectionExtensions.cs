// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Implementations;
using MQTTnet.Server;

namespace MQTTnet.AspNetCore
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMqttServer(this IServiceCollection serviceCollection, Action<MqttServerOptionsBuilder> configure = null)
        {
            if (serviceCollection is null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            serviceCollection.AddMqttConnectionHandler();
            serviceCollection.AddHostedMqttServer(configure);
            
            return serviceCollection;
        }

        public static IServiceCollection AddHostedMqttServer(this IServiceCollection services, MqttServerOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            services.AddSingleton(options);

            services.AddHostedMqttServer();

            return services;
        }

        public static IServiceCollection AddHostedMqttServer(this IServiceCollection services, Action<MqttServerOptionsBuilder> configure = null)
        {
            services.AddSingleton(s =>
            {
                var serverOptionsBuilder = new MqttServerOptionsBuilder();
                configure?.Invoke(serverOptionsBuilder);
                return serverOptionsBuilder.Build();
            });

            services.AddHostedMqttServer();

            return services;
        }

        public static IServiceCollection AddHostedMqttServerWithServices(this IServiceCollection services, Action<AspNetMqttServerOptionsBuilder> configure)
        {
            services.AddSingleton(s =>
            {
                var builder = new AspNetMqttServerOptionsBuilder(s);
                configure(builder);
                return builder.Build();
            });

            services.AddHostedMqttServer();

            return services;
        }

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
