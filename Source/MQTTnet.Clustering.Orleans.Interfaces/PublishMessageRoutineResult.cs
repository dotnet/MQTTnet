using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Clustering.Orleans
{
    public class PublishMessageRoutineResult
    {

        public required ulong RoutineId { get; init; }

        public required PublishMessageResult Result { get; init; }

    }
}
