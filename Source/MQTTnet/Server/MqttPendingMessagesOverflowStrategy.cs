namespace MQTTnet.Server
{
    public enum MqttPendingMessagesOverflowStrategy
    {
        DropOldestQueuedMessage,
        DropNewMessage
    }
}
