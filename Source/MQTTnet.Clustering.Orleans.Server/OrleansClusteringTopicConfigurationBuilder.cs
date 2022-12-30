using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Clustering.Orleans.Server.Behaviors;
using MQTTnet.Clustering.Orleans.Server.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Clustering.Orleans.Server;

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

    public void RestrictToSingleClient(string? clientIdFormat = null, string? nodeGroup = null)
    {
        if (_topicFilter == null)
        {

            //_clusteredTopicRelayService._defaultBehavior = new()
            //{
                
            //    Handler = 
            //};
        }
    }

    public void RelayToManyClients(string? topicFormat = null, string? nodeGroup = null)
    {
        if (_topicFilter == null)
        {

            //_clusteredTopicRelayService._defaultBehavior = new()
            //{

            //    Handler = 
            //};
        }
    }

    public void RelayAll(string? nodeGroupName = null, OrleansClusteringRelayStrategy strategy = OrleansClusteringRelayStrategy.SendToAllNodes)
    {

    }

    public void RestrictToGrain<TGrain>(Func<OrleansClusteringGrainKeyBuilder, ValueTask<string>> keySelector, Func<OrleansClusteringGrainContext<string, TGrain>, Task> handler) where TGrain : IGrainWithStringKey
    {
        
    }

    public void RestrictToGrain<TGrain>(Func<OrleansClusteringGrainKeyBuilder, string> keySelector, Func<OrleansClusteringGrainContext<string, TGrain>, Task> handler) where TGrain : IGrainWithStringKey
        => RestrictToGrain(b => new ValueTask<string>(keySelector(b)), handler);

    public void RestrictToGrain<TGrain>(Func<OrleansClusteringGrainContext<string, TGrain>, Task> handler) where TGrain : IGrainWithStringKey
        => RestrictToGrain(b => b.ClientId, handler);

    public void RestrictToGrain<TGrain>(Func<OrleansClusteringGrainKeyBuilder, ValueTask<int>> keySelector, Func<OrleansClusteringGrainContext<int, TGrain>, Task> handler) where TGrain : IGrainWithIntegerKey
    {

    }

    public void RestrictToGrain<TGrain>(Func<OrleansClusteringGrainKeyBuilder, int> keySelector, Func<OrleansClusteringGrainContext<int, TGrain>, Task> handler) where TGrain : IGrainWithIntegerKey
        => RestrictToGrain(b => new ValueTask<int>(keySelector(b)), handler);

    public void RestrictToGrain<TGrain>(Func<OrleansClusteringGrainContext<int, TGrain>, Task> handler) where TGrain : IGrainWithIntegerKey
        => RestrictToGrain(b => int.Parse(b.ClientId), handler);

    public void RestrictToGrain<TGrain>(Func<OrleansClusteringGrainKeyBuilder, ValueTask<Guid>> keySelector, Func<OrleansClusteringGrainContext<Guid, TGrain>, Task> handler) where TGrain : IGrainWithGuidKey
    {

    }

    public void RestrictToGrain<TGrain>(Func<OrleansClusteringGrainKeyBuilder, Guid> keySelector, Func<OrleansClusteringGrainContext<Guid, TGrain>, Task> handler) where TGrain : IGrainWithGuidKey
        => RestrictToGrain(b => new ValueTask<Guid>(keySelector(b)), handler);

    public void RestrictToGrain<TGrain>(Func<OrleansClusteringGrainContext<Guid, TGrain>, Task> handler) where TGrain : IGrainWithGuidKey
        => RestrictToGrain(b => Guid.Parse(b.ClientId), handler);

    public void RestrictToGrain<TGrain>(Func<OrleansClusteringGrainKeyBuilder, ValueTask<(int, string)>> keySelector, Func<OrleansClusteringGrainContext<(int Integer, string String), TGrain>, Task> handler) where TGrain : IGrainWithIntegerCompoundKey
    {

    }

    public void RestrictToGrain<TGrain>(Func<OrleansClusteringGrainKeyBuilder, (int, string)> keySelector, Func<OrleansClusteringGrainContext<(int Integer, string String), TGrain>, Task> handler) where TGrain : IGrainWithIntegerCompoundKey
        => RestrictToGrain(b => new ValueTask<(int, string)>(keySelector(b)), handler);

    public void RestrictToGrain<TGrain>(Func<OrleansClusteringGrainKeyBuilder, ValueTask<(Guid, string)>> keySelector, Func<OrleansClusteringGrainContext<(Guid Guid, string String), TGrain>, Task> handler) where TGrain : IGrainWithGuidCompoundKey
    {

    }

    public void RestrictToGrain<TGrain>(Func<OrleansClusteringGrainKeyBuilder, (Guid, string)> keySelector, Func<OrleansClusteringGrainContext<(Guid Guid, string String), TGrain>, Task> handler) where TGrain : IGrainWithGuidCompoundKey
        => RestrictToGrain(b => new ValueTask<(Guid, string)>(keySelector(b)), handler);




    public void AggregateToGrain<TGrain>(Func<OrleansClusteringGrainKeyBuilder, ValueTask<string>> keySelector, Func<OrleansClusteringAggregatedGrainContext<string, TGrain>, Task> handler, TimeSpan interval = default) where TGrain : IGrainWithStringKey
    {

    }

    public void AggregateToGrain<TGrain>(Func<OrleansClusteringGrainKeyBuilder, string> keySelector, Func<OrleansClusteringAggregatedGrainContext<string, TGrain>, Task> handler, TimeSpan interval = default) where TGrain : IGrainWithStringKey
        => AggregateToGrain(b => new ValueTask<string>(keySelector(b)), handler);

    public void AggregateToGrain<TGrain>(Func<OrleansClusteringAggregatedGrainContext<string, TGrain>, Task> handler, TimeSpan interval = default) where TGrain : IGrainWithStringKey
        => AggregateToGrain(b => b.ClientId, handler);

    public void AggregateToGrain<TGrain>(Func<OrleansClusteringGrainKeyBuilder, ValueTask<int>> keySelector, Func<OrleansClusteringAggregatedGrainContext<int, TGrain>, Task> handler, TimeSpan interval = default) where TGrain : IGrainWithIntegerKey
    {

    }

    public void AggregateToGrain<TGrain>(Func<OrleansClusteringGrainKeyBuilder, int> keySelector, Func<OrleansClusteringAggregatedGrainContext<int, TGrain>, Task> handler, TimeSpan interval = default) where TGrain : IGrainWithIntegerKey
        => AggregateToGrain(b => new ValueTask<int>(keySelector(b)), handler);

    public void AggregateToGrain<TGrain>(Func<OrleansClusteringAggregatedGrainContext<int, TGrain>, Task> handler, TimeSpan interval = default) where TGrain : IGrainWithIntegerKey
        => AggregateToGrain(b => int.Parse(b.ClientId), handler);

    public void AggregateToGrain<TGrain>(Func<OrleansClusteringGrainKeyBuilder, ValueTask<Guid>> keySelector, Func<OrleansClusteringAggregatedGrainContext<Guid, TGrain>, Task> handler, TimeSpan interval = default) where TGrain : IGrainWithGuidKey
    {

    }

    public void AggregateToGrain<TGrain>(Func<OrleansClusteringGrainKeyBuilder, Guid> keySelector, Func<OrleansClusteringAggregatedGrainContext<Guid, TGrain>, Task> handler, TimeSpan interval = default) where TGrain : IGrainWithGuidKey
        => AggregateToGrain(b => new ValueTask<Guid>(keySelector(b)), handler);

    public void AggregateToGrain<TGrain>(Func<OrleansClusteringAggregatedGrainContext<Guid, TGrain>, Task> handler, TimeSpan interval = default) where TGrain : IGrainWithGuidKey
        => AggregateToGrain(b => Guid.Parse(b.ClientId), handler);

    public void AggregateToGrain<TGrain>(Func<OrleansClusteringGrainKeyBuilder, ValueTask<(int, string)>> keySelector, Func<OrleansClusteringAggregatedGrainContext<(int Integer, string String), TGrain>, Task> handler, TimeSpan interval = default) where TGrain : IGrainWithIntegerCompoundKey
    {

    }

    public void AggregateToGrain<TGrain>(Func<OrleansClusteringGrainKeyBuilder, (int, string)> keySelector, Func<OrleansClusteringAggregatedGrainContext<(int Integer, string String), TGrain>, Task> handler, TimeSpan interval = default) where TGrain : IGrainWithIntegerCompoundKey
        => AggregateToGrain(b => new ValueTask<(int, string)>(keySelector(b)), handler);

    public void AggregateToGrain<TGrain>(Func<OrleansClusteringGrainKeyBuilder, ValueTask<(Guid, string)>> keySelector, Func<OrleansClusteringAggregatedGrainContext<(Guid Guid, string String), TGrain>, Task> handler, TimeSpan interval = default) where TGrain : IGrainWithGuidCompoundKey
    {

    }

    public void AggregateToGrain<TGrain>(Func<OrleansClusteringGrainKeyBuilder, (Guid, string)> keySelector, Func<OrleansClusteringAggregatedGrainContext<(Guid Guid, string String), TGrain>, Task> handler, TimeSpan interval = default) where TGrain : IGrainWithGuidCompoundKey
        => AggregateToGrain(b => new ValueTask<(Guid, string)>(keySelector(b)), handler);

}
