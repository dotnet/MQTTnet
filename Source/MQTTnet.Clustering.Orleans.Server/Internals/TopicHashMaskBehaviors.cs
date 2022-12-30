using MQTTnet.Clustering.Orleans.Server.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Clustering.Orleans.Server.Internals
{
    public sealed class TopicHashMaskBehaviors
    {

        public Dictionary<ulong, HashSet<ClusteredRelayBehaviorWithTopic>> BehaviorsByHashMask { get; } = new Dictionary<ulong, HashSet<ClusteredRelayBehaviorWithTopic>>();

        public void AddBehavior(ClusteredRelayBehaviorWithTopic behavior)
        {
            if (!BehaviorsByHashMask.TryGetValue(behavior.TopicHashMask, out var behaviors))
            {
                behaviors = new HashSet<ClusteredRelayBehaviorWithTopic>();
                BehaviorsByHashMask.Add(behavior.TopicHashMask, behaviors);
            }
            behaviors.Add(behavior);
        }

        public void RemoveBehavior(ClusteredRelayBehaviorWithTopic behavior)
        {
            if (BehaviorsByHashMask.TryGetValue(behavior.TopicHashMask, out var behaviors))
            {
                behaviors.Remove(behavior);
                if (behaviors.Count <= 0)
                {
                    BehaviorsByHashMask.Remove(behavior.TopicHashMask);
                }
            }
        }

    }
}
