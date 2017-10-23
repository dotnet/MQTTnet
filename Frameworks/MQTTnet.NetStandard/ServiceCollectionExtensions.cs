using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Client;
using MQTTnet.Core.ManagedClient;
using MQTTnet.Core.Serializer;
using MQTTnet.Core.Server;
using MQTTnet.Implementations;
using System;
using Microsoft.Extensions.Logging;
using MQTTnet.Core.Diagnostics;

namespace MQTTnet
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMqttServer(this IServiceCollection services)
        {
            services.AddOptions();
            services.AddSingleton<MqttFactory>();
            services.AddSingleton<IMqttCommunicationAdapterFactory, MqttFactory>();
            services.AddSingleton<IMqttClientSesssionFactory, MqttFactory>();


            services.AddSingleton<IMqttServer,MqttServer>();
            services.AddSingleton<MqttServer>();

            services.AddTransient<IMqttServerAdapter, MqttServerAdapter>();
            services.AddTransient<IMqttPacketSerializer, MqttPacketSerializer>();

            services.AddTransient<MqttClientSessionsManager>();
            services.AddTransient<MqttClientRetainedMessagesManager>();
            return services;
        }

        public static IServiceCollection AddMqttServer(this IServiceCollection services, Action<MqttServerOptions> configureOptions)
        {
            return services
                .AddMqttServer()
                .Configure(configureOptions);
        }

        public static IServiceCollection AddMqttClient(this IServiceCollection services)
        {
            services.AddSingleton<MqttFactory>();
            services.AddSingleton<IMqttCommunicationAdapterFactory, MqttFactory>();

            services.AddTransient<IMqttClient, MqttClient>();
            services.AddTransient<MqttClient>();
            services.AddTransient<IMqttPacketSerializer, MqttPacketSerializer>();
            services.AddTransient<ManagedMqttClient>();
            services.AddTransient<MqttPacketDispatcher>();

            return services;
        }

        public static ILoggerFactory AddMqttTrace(this ILoggerFactory factory)
        {
            factory.AddProvider(new MqttNetTrace());
            return factory;
        }
    }
}
