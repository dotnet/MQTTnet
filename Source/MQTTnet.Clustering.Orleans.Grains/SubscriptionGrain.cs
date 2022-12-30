using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Clustering.Orleans.Grains
{
    public class SubscriptionGrain : Grain, ISubscriptionGrain
    {

        public Task AddClient(string clientId)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetClients()
        {
            throw new NotImplementedException();
        }

        public Task RemoveClient(string clientId)
        {
            throw new NotImplementedException();
        }

    }
}
