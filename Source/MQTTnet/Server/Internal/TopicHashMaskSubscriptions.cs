using System.Collections.Generic;

namespace MQTTnet.Server
{
    /// <summary>
    ///     Helper class that stores the topic hash mask common to all contained Subscriptions for direct access.
    /// </summary>
    public sealed class TopicHashMaskSubscriptions
    {
        public TopicHashMaskSubscriptions(ulong hashMask)
        {
            HashMask = hashMask;
        }

        public ulong HashMask { get; }

        public HashSet<MqttSubscription> Subscriptions { get; } = new HashSet<MqttSubscription>();
    }
}