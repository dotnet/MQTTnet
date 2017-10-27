using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Server;

namespace MQTTnet.AspNetCore
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHostedMqttServer(this IServiceCollection services)
        {
            services.AddMqttServerServices();

            services.AddSingleton<IHostedService>(s => s.GetService<MqttHostedServer>());
            services.AddSingleton<IMqttServer>(s => s.GetService<MqttHostedServer>());
            services.AddSingleton<MqttHostedServer>();
            
            services.AddSingleton<MqttWebSocketServerAdapter>();
            services.AddSingleton<IMqttServerAdapter>(s => s.GetService<MqttWebSocketServerAdapter>());

            return services;
        }
    }
}
