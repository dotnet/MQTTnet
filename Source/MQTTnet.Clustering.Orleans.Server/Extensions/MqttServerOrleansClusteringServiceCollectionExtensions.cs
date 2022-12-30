using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Clustering.Orleans.Server.Services;

namespace Microsoft.AspNetCore.Builder;

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
