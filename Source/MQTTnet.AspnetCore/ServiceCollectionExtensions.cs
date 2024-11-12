// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MQTTnet.Adapter;
using MQTTnet.AspNetCore.Internal;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Server;
using MQTTnet.Server.Internal.Adapter;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace MQTTnet.AspNetCore;

public static class ServiceCollectionExtensions
{
    const string SocketConnectionFactoryTypeName = "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.SocketConnectionFactory";
    const string SocketConnectionFactoryAssemblyName = "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets";

    public static IServiceCollection AddMqttServer(this IServiceCollection services, Action<MqttServerOptionsBuilder> configure, Action<MqttServerStopOptionsBuilder> stopConfigure)
    {
        services.AddOptions<MqttServerStopOptionsBuilder>().Configure(stopConfigure);
        return services.AddMqttServer(configure);
    }

    public static IServiceCollection AddMqttServer(this IServiceCollection services, Action<MqttServerOptionsBuilder> configure)
    {
        services.AddOptions<MqttServerOptionsBuilder>().Configure(configure);
        return services.AddMqttServer();
    }

    public static IServiceCollection AddMqttServer(this IServiceCollection services)
    {
        services.AddConnections();
        services.TryAddSingleton<IMqttNetLogger>(MqttNetNullLogger.Instance);

        var mqttConnectionHandler = new MqttConnectionHandler();
        services.TryAddSingleton(mqttConnectionHandler);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMqttServerAdapter>(mqttConnectionHandler));

        services.TryAddSingleton<AspNetCoreMqttServer>();
        services.TryAddSingleton<MqttServer>(s => s.GetRequiredService<AspNetCoreMqttServer>());
        services.AddHostedService<AspNetCoreMqttHostedServer>();

        return services.AddOptions();
    }

    public static IServiceCollection AddMqttTcpServerAdapter(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMqttServerAdapter, MqttTcpServerAdapter>());
        return services;
    }


    [DynamicDependency(DynamicallyAccessedMemberTypes.All, SocketConnectionFactoryTypeName, SocketConnectionFactoryAssemblyName)]
    public static IServiceCollection AddMqttClientAdapterFactory(this IServiceCollection services)
    {
        var socketConnectionFactoryType = Assembly.Load(SocketConnectionFactoryAssemblyName).GetType(SocketConnectionFactoryTypeName);
        services.AddSingleton(typeof(IConnectionFactory), socketConnectionFactoryType);
        services.TryAddSingleton<AspNetCoreMqttClientAdapterFactory>();
        services.TryAddSingleton<IMqttClientAdapterFactory>(s => s.GetRequiredService<AspNetCoreMqttClientAdapterFactory>());
        return services;
    }
}