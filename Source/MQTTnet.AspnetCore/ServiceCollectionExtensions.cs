// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Server;
using MQTTnet.Server.Internal.Adapter;

namespace MQTTnet.AspNetCore;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHostedMqttServer(this IServiceCollection services, MqttServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.AddSingleton(options);
        services.AddHostedMqttServer();

        return services;
    }

    public static IServiceCollection AddHostedMqttServer(this IServiceCollection services, Action<MqttServerOptionsBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);

        var serverOptionsBuilder = new MqttServerOptionsBuilder();

        configure?.Invoke(serverOptionsBuilder);

        var options = serverOptionsBuilder.Build();

        return AddHostedMqttServer(services, options);
    }

    public static void AddHostedMqttServer(this IServiceCollection services)
    {
        // The user may have these services already registered.
        services.TryAddSingleton<IMqttNetLogger>(MqttNetNullLogger.Instance);
        services.TryAddSingleton(new MqttServerFactory());

        services.AddSingleton<MqttHostedServer>();
        services.AddHostedService(s => s.GetService<MqttHostedServer>());
        services.AddSingleton(s => s.GetService<MqttHostedServer>().MqttServer);

    }

    public static IServiceCollection AddHostedMqttServerWithServices(this IServiceCollection services, Action<AspNetMqttServerOptionsBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);

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
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(logger);
    }

    public static IServiceCollection AddMqttServer(this IServiceCollection serviceCollection, Action<MqttServerOptionsBuilder> configure = null)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);

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