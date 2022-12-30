using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Clustering.Orleans.Server.Extensions
{
    public static class MqttServerOrleansClusteringHostBuilderExtensions
    {

        public static IHostBuilder UseOrleansClustering(this IHostBuilder hostBuilder)
        {

            hostBuilder.ConfigureServices(services =>
            {
                services.AddMqttServerOrleansClustering();
            });

            hostBuilder.

            return hostBuilder;
        }

    }
}
