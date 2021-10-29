namespace MQTTnet.Server
{
    public enum MqttTopicFilterCompareResult
    {
        NoMatch,
        
        IsMatch,
        
        FilterInvalid,
        
        TopicInvalid
    }
}