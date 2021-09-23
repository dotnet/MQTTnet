namespace MQTTnet.Server
{
    public sealed class GetSubscribedMessagesFilter
    {
        public bool IsNewSubscription { get; set; }
        
        public MqttTopicFilter TopicFilter { get; set; }
    }
}