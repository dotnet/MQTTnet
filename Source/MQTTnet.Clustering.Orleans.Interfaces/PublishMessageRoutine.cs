using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Clustering.Orleans
{
    public class PublishMessageRoutine
    {

        public required ulong RoutineId { get; init; }

        public required ClusterApplicationMessage Message { get; init; }

    }
}
