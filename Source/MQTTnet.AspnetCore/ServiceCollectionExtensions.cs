// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Implementations;
using MQTTnet.Server;

namespace MQTTnet.AspNetCore
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHostedMqttServer(this IServiceCollection services, MqttServerOptions options)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            services.AddSingleton(options);
            services.AddHostedMqttServer();

            return services;
        }

        public static IServiceCollection AddHostedMqttServer(this IServiceCollection services, Action<MqttServerOptionsBuilder> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var serverOptionsBuilder = new MqttServerOptionsBuilder();
            configure.Invoke(serverOptionsBuilder);
            var options = serverOptionsBuilder.Build();

            return AddHostedMqttServer(services, options);
        }

        public static void AddHostedMqttServer(this IServiceCollection services)
        {
            // The user may have these services already registered.
            services.TryAddSingleton<IMqttNetLogger>(MqttNetNullLogger.Instance);
            services.TryAddSingleton(new MqttFactory());

            services.AddSingleton<MqttHostedServer>();
            services.AddHostedService<MqttHostedServer>();
        }

        public static IServiceCollection AddHostedMqttServerWithServices(this IServiceCollection services, Action<AspNetMqttServerOptionsBuilder> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton(
                s =>
                {
                    var builder = new AspNetMqttServerOptionsBuilder(s);
                    configure(builder);
                    return builder.Build();
                });

            services.AddHostedMqttServer();

            return services;
        }

        public static IServiceCollection AddMqttConnectionHandler(this IServiceCollection services)
        {
            services.AddSingleton<MqttConnectionHandler>();
            services.AddSingleton<IMqttServerAdapter>(s => s.GetService<MqttConnectionHandler>());

            return services;
        }

        public static void AddMqttLogger(this IServiceCollection services, IMqttNetLogger logger)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton(logger);
        }

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

        public static IServiceCollection AddMqttTcpServerAdapter(this IServiceCollection services)
        {
            services.AddSingleton<MqttTcpServerAdapter>();
            services.AddSingleton<IMqttServerAdapter>(s => s.GetService<MqttTcpServerAdapter>());

            return services;
        }

        public static IServiceCollection AddMqttWebSocketServerAdapter(this IServiceCollection services)
        {
            services.AddSingleton<MqttWebSocketServerAdapter>();
            services.AddSingleton<IMqttServerAdapter>(s => s.GetService<MqttWebSocketServerAdapter>());

            return services;
        }
    }
}