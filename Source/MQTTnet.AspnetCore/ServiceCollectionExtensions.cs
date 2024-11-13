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

    /// <summary>
    /// Register MqttServer as a service
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configure">serverOptions configure</param>
    /// <param name="stopConfigure">server stop configure</param>
    /// <returns></returns>
    public static IServiceCollection AddMqttServer(
        this IServiceCollection services,
        Action<MqttServerOptionsBuilder> configure,
        Action<MqttServerStopOptionsBuilder> stopConfigure)
    {
        services.AddOptions<MqttServerStopOptionsBuilder>().Configure(stopConfigure);
        return services.AddMqttServer(configure);
    }

    /// <summary>
    /// Register MqttServer as a service
    /// </summary>
    /// <param name="services">serverOptions configure</param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IServiceCollection AddMqttServer(
        this IServiceCollection services,
        Action<MqttServerOptionsBuilder> configure)
    {
        services.AddOptions<MqttServerOptionsBuilder>().Configure(configure);
        return services.AddMqttServer();
    }

    /// <summary>
    /// Register MqttServer as a service
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddMqttServer(this IServiceCollection services)
    {
        services.AddOptions();
        services.AddConnections();
        services.AddLogging();
        services.TryAddSingleton<IMqttNetLogger, AspNetCoreMqttNetLogger>();

        services.TryAddSingleton<MqttConnectionHandler>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMqttServerAdapter, AspNetCoreMqttServerAdapter>());

        services.TryAddSingleton<AspNetCoreMqttServer>();
        services.AddHostedService<AspNetCoreMqttHostedServer>();
        services.TryAddSingleton<MqttServer>(s => s.GetRequiredService<AspNetCoreMqttServer>());

        return services;
    }

    /// <summary>
    /// Register MqttTcpServerAdapter as a IMqttServerAdapter    
    /// </summary>
    /// <remarks>We recommend using ListenOptions.UseMqtt() instead of using MqttTcpServerAdapter in an AspNetCore environment</remarks>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddMqttTcpServerAdapter(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMqttServerAdapter, MqttTcpServerAdapter>());
        return services;
    }

    /// <summary>
    /// Register IMqttClientAdapterFactory as a service
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, SocketConnectionFactoryTypeName, SocketConnectionFactoryAssemblyName)]
    public static IServiceCollection AddMqttClientAdapterFactory(this IServiceCollection services)
    {
        var socketConnectionFactoryType = Assembly.Load(SocketConnectionFactoryAssemblyName).GetType(SocketConnectionFactoryTypeName);
        services.TryAddSingleton(typeof(IConnectionFactory), socketConnectionFactoryType);
        services.TryAddSingleton<IMqttClientAdapterFactory, AspNetCoreMqttClientAdapterFactory>();
        return services;
    }
}