using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Clustering.Orleans.Server.Behaviors
{
    public class ClusteredRelayBehavior
    {

        public required Func<InterceptingPublishEventArgs, Task> Handler { get; init; }

    }
}
