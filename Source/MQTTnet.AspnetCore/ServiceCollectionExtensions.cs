// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Server;

namespace MQTTnet.AspNetCore;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register MqttServer as a singleton service
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IMqttServerBuilder AddMqttServer(this IServiceCollection services)
    {
        services.AddOptions();
        services.AddConnections();
        services.TryAddSingleton<MqttConnectionHandler>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMqttServerAdapter, AspNetCoreMqttServerAdapter>());

        services.TryAddSingleton<AspNetCoreMqttServer>();
        services.AddHostedService<AspNetCoreMqttHostedServer>();
        services.TryAddSingleton<MqttServer>(s => s.GetRequiredService<AspNetCoreMqttServer>());

        return services.AddMqtt();
    }

    /// <summary>
    /// Register IMqttClientFactory as a singleton service
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IMqttClientBuilder AddMqttClient(this IServiceCollection services)
    {
        services.TryAddSingleton<IMqttClientAdapterFactory, AspNetCoreMqttClientAdapterFactory>();
        services.TryAddSingleton<IMqttClientFactory, AspNetCoreMqttClientFactory>();
        return services.AddMqtt();
    }

    private static MqttBuilder AddMqtt(this IServiceCollection services)
    {
        services.AddLogging();
        services.TryAddSingleton<IMqttNetLogger, AspNetCoreMqttNetLogger>();
        return new MqttBuilder(services);
    }

    private class MqttBuilder(IServiceCollection services) : IMqttServerBuilder, IMqttClientBuilder
    {
        public IServiceCollection Services { get; } = services;
    }
}