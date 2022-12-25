using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Clustering.Orleans.Server;
using MQTTnet.Clustering.Orleans.Server.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public static class MqttServerOrleansClusteringServiceCollectionExtensions
    {

        public static IServiceCollection AddMqttServerOrleansClustering(this IServiceCollection services)
        {
            services
                .AddSingleton<ConnectedClientManagementService>()
                .AddSingleton<ClusteredTopicRelayService>();

            return services;
        }

    }
}
