using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Clustering.Orleans.Server.Behaviors
{
    public class RelayToGrainBehaviorBuilder<TGrainKey, TGrain>
    {


        public RelayToGrainBehaviorBuilder(IReadOnlyDictionary<string, string> arguments, MqttApplicationMessage message)
        {
            Arguments = arguments;
            Message = message;
        }

        public IReadOnlyDictionary<string, string> Arguments { get; }

        public MqttApplicationMessage Message { get; }

        public async Task CallGrainAsync(TGrainKey grainKey, Func<TGrain, Task> handler)
        {

        }

    }
}
