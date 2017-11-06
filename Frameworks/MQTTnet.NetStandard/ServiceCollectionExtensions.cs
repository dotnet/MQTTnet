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
            services.AddMqttServerServices();
            
            services.AddSingleton<IMqttServer>(s => s.GetService<MqttServer>());
            services.AddSingleton<MqttServer>();

            return services;
        }

        public static IServiceCollection AddMqttServerServices(this IServiceCollection services)
        {
            services.AddOptions();
            services.AddSingleton<MqttFactory>();
            services.AddSingleton<IMqttCommunicationAdapterFactory>(s => s.GetService<MqttFactory>());
            services.AddSingleton<IMqttClientSesssionFactory>(s => s.GetService<MqttFactory>());

            services.AddTransient<IMqttServerAdapter, MqttServerAdapter>();
            services.AddTransient<IMqttPacketSerializer, MqttPacketSerializer>();

            services.AddSingleton<MqttClientSessionsManager>();
            services.AddTransient<MqttClientSubscriptionsManager>();
            services.AddSingleton<IMqttClientRetainedMessageManager, MqttClientRetainedMessagesManager>();
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
            services.AddSingleton<IMqttCommunicationAdapterFactory>(s => s.GetService<MqttFactory>());

            services.AddTransient<IMqttClient, MqttClient>();
            services.AddTransient<MqttClient>();
            services.AddTransient<IManagedMqttClient, ManagedMqttClient>();
            services.AddTransient<ManagedMqttClient>();
            services.AddTransient<IMqttPacketSerializer, MqttPacketSerializer>();
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
