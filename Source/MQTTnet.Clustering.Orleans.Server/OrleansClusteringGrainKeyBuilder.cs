using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Clustering.Orleans.Server
{
    public class OrleansClusteringGrainKeyBuilder
    {

        public required string ClientId { get; init; }

        public required IDictionary SessionItems { get; init; }

        public required IReadOnlyDictionary<string, string> Arguments { get; init; }

        public required MqttApplicationMessage Message { get; init; }

    }
}
