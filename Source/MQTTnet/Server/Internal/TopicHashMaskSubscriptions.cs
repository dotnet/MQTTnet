using System;
using System.Collections.Generic;
using System.Text;

namespace MQTTnet.Server
{
    /// <summary>
    /// Helper class that stores the topic hash mask common to all contained Subscriptions for direct access.
    /// </summary>
    public class TopicHashMaskSubscriptions
    {
        public TopicHashMaskSubscriptions(ulong hashMask)
        {
            HashMask = hashMask;
            Subscriptions = new HashSet<MqttSubscription>();
        }
        public ulong HashMask { get; private set; }
        public HashSet<MqttSubscription> Subscriptions { get; private set; }
    }
}
