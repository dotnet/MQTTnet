using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Clustering.Orleans.Grains
{
    [Reentrant]
    public class SessionGrain : Grain, ISessionGrain
    {
        private readonly IGrainFactory _grainFactory;

        public SessionGrain(IGrainFactory grainFactory)
        {
            _grainFactory = grainFactory;
        }

        public async ValueTask<PublishMessageResult> PublishMessageFromClient(ClusterApplicationMessage message)
        {
            var publishMessageGrain = _grainFactory.GetGrain<IPublishMessageGrain>(0);
            var result = await publishMessageGrain.PublishMessageAcrossNodes(message);
            return result;
        }
    }
}
