using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.AspNetCore;
using MQTTnet.Clustering.Orleans.Server;
using MQTTnet.Clustering.Orleans.Server.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Server
{
    public static class MqttServerOrleansClusteringApplicationBuilderExtensions
    {

        public static IApplicationBuilder UseMqttOrleansClustering(this IApplicationBuilder applicationBuilder, Action<OrleansClusteringConfigurationBuilder> config)
        {
            var builder = new OrleansClusteringConfigurationBuilder(applicationBuilder);
            var clusteredTopicRelayService = applicationBuilder.ApplicationServices.GetRequiredService<ClusteredTopicRelayService>();
            applicationBuilder.UseMqttServer(mqttServer =>
            {
                mqttServer.InterceptingPublishAsync += clusteredTopicRelayService.OnInterceptingPublishAsync;
                mqttServer.ClientSubscribedTopicAsync += clusteredTopicRelayService.OnClientSubscribedTopicAsync;
                mqttServer.ClientUnsubscribedTopicAsync += clusteredTopicRelayService.OnClientUnsubscribedTopicAsync;
            });
            config(builder);
            return applicationBuilder;
        }

    }
}
