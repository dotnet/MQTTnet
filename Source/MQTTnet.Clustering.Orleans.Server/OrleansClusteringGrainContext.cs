using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Clustering.Orleans.Server
{
    public class OrleansClusteringGrainContext<TKey, TGrain>
    {

        public required TKey Key { get; init; }

        public required TGrain Grain { get; init; }

        public required IReadOnlyDictionary<string, string> Arguments { get; init; }

        public required MqttApplicationMessage Message { get; init; }

    }
}
