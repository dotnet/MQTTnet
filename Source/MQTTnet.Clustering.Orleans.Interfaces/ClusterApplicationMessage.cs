using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Clustering.Orleans
{
    public class ClusterApplicationMessage
    {

        public required string SenderClientId { get; init; }

        public required string Topic { get; init; }

        public required byte[] Payload { get; init; }

    }
}
