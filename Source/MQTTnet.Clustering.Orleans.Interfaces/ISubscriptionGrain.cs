using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Clustering.Orleans
{
    public interface ISubscriptionGrain : IGrainWithStringKey
    {

        Task AddClient(string clientId);

        Task RemoveClient(string clientId);

        Task<string> GetClients();

    }
}
