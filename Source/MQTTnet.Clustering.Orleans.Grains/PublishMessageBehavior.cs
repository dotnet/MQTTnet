using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Clustering.Orleans.Grains
{
    public abstract class PublishMessageBehavior
    {

        public abstract ValueTask<string> GetTargetNodeGroupNameAsync(ClusterApplicationMessage applicationMessage);

    }
}
