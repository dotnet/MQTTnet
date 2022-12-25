using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Clustering.Orleans.Server.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Clustering.Orleans.Server
{
    public class OrleansClusteringTopicConfigurationBuilder
    {
        private readonly IApplicationBuilder _applicationBuilder;
        private readonly ClusteredTopicRelayService _clusteredTopicRelayService;
        private readonly string? _topicFilter;

        public OrleansClusteringTopicConfigurationBuilder(IApplicationBuilder applicationBuilder, ClusteredTopicRelayService clusteredTopicRelayService, string? topicFilter)
        {
            _applicationBuilder = applicationBuilder;
            _clusteredTopicRelayService = clusteredTopicRelayService;
            _topicFilter = topicFilter;
        }

        public void OneToOne()
        {
            if (_topicFilter == null)
            {
                //_clusteredTopicRelayService._defaultBehavior = new()
                //{
                    
                //    Handler = 
                //};
            }
        }

        public void RelayToAll(OrleansClusteringRelayStrategy strategy = default)
        {

        }

    }
}
