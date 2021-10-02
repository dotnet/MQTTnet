namespace MQTTnet.Server.Internal
{
    public enum MqttTopicFilterCompareResult
    {
        NoMatch,
        
        IsMatch,
        
        FilterInvalid,
        
        TopicInvalid
    }
}