namespace MQTTnet.Server.Internal
{
    public interface ISubscriptionChangedNotification
    {
        void OnSubscriptionsAdded(MqttSession clientSession, List<MqttSubscription> subscriptionsTopics);

        void OnSubscriptionsRemoved(MqttSession clientSession, List<string> subscriptionTopics);
    }
}