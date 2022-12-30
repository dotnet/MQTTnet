using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Clustering.Orleans.Server;

public class OrleansClusteringAggregatedGrainContext<TKey, TGrain> where TGrain : IGrain
{
    
    public required TKey Key { get; init; }

    public required TGrain Grain { get; init; }

    public required IReadOnlyList<OrleansClusteringAggregatedItemContext> Items { get; init; }

}
