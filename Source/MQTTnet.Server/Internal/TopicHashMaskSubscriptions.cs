namespace MQTTnet.Server.Internal
{
    /// <summary>
    ///     Helper class that stores subscriptions by their topic hash mask.
    /// </summary>
    public sealed class TopicHashMaskSubscriptions
    {
        public Dictionary<ulong, HashSet<MqttSubscription>> SubscriptionsByHashMask { get; } = new Dictionary<ulong, HashSet<MqttSubscription>>();

        public void AddSubscription(MqttSubscription subscription)
        {
            if (!SubscriptionsByHashMask.TryGetValue(subscription.TopicHashMask, out var subscriptions))
            {
                subscriptions = new HashSet<MqttSubscription>();
                SubscriptionsByHashMask.Add(subscription.TopicHashMask, subscriptions);
            }
            subscriptions.Add(subscription);
        }

        public void RemoveSubscription(MqttSubscription subscription)
        {
            if (SubscriptionsByHashMask.TryGetValue(subscription.TopicHashMask, out var subscriptions))
            {
                subscriptions.Remove(subscription);
                if (subscriptions.Count == 0)
                {
                    SubscriptionsByHashMask.Remove(subscription.TopicHashMask);
                }
            }
        }
    }
}