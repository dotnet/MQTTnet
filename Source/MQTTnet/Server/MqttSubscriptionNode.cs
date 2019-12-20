namespace MQTTnet.Server
{
    public class MqttSubscriptionNode
    {
        public string Id { get; }
        public TopicFilter TopicFilter { get; set; }
        public MqttSubscriptionIndex Children { get; }

        public bool IsMultiLevelWildcard => Id.Length == 1 && Id[0] == MqttTopicFilterComparer.MultiLevelWildcard;

        public MqttSubscriptionNode(string id)
        {
            Id = id;
            Children = new MqttSubscriptionIndex();
        }
    }
}
