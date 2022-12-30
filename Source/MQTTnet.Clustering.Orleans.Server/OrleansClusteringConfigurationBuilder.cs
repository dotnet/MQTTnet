using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Clustering.Orleans.Server
{
    public class OrleansClusteringConfigurationBuilder
    {
        public OrleansClusteringConfigurationBuilder(IApplicationBuilder applicationBuilder)
        {
            ApplicationBuilder = applicationBuilder;
        }

        public IApplicationBuilder ApplicationBuilder { get; }

        public OrleansClusteringConfigurationBuilder ForTopic(string topic, Action<OrleansClusteringTopicConfigurationBuilder> configHandler)
        {

            return this;
        }

        public OrleansClusteringConfigurationBuilder DefaultTopicBehavior(Action<OrleansClusteringTopicConfigurationBuilder> configHandler)
        {

            return this;
        }

    }
}
