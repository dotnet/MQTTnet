using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Clustering.Orleans.Grains
{
    [Reentrant]
    public class ConnectedClientGrain : Grain, IConnectedClientGrain
    {
        public ValueTask ConnectClient()
        {
            throw new NotImplementedException();
        }

        public ValueTask DisconnectClient()
        {
            throw new NotImplementedException();
        }
    }
}
