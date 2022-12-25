using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Clustering.Orleans.Server.Behaviors
{
    public class ClusteredRelayBehaviorWithTopic : ClusteredRelayBehavior
    {

        public ClusteredRelayBehaviorWithTopic()
        {
            MqttSubscription.CalculateTopicHash(Topic, out var hash, out var hashMask, out var hasWildcard);
            TopicHash = hash;
            TopicHashMask = hashMask;
            TopicHasWildcard = hasWildcard;
        }

        public required string Topic { get; init; }

        public ulong TopicHash { get; }

        public ulong TopicHashMask { get; }

        public bool TopicHasWildcard { get; }

    }
}
