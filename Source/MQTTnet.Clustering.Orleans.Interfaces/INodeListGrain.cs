using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Clustering.Orleans
{
    public interface INodeListGrain : IGrainWithStringKey
    {

        ValueTask AddNode(SiloAddress host);

        ValueTask<HashSet<SiloAddress>> GetNodes();

    }
}
