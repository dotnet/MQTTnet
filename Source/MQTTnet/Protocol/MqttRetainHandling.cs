namespace MQTTnet.Protocol
{
    public enum MqttRetainHandling
    {
        SendAtSubscribe = 0,
        SendAtSubscribeIfNewSubscriptionOnly = 1,
        DoNotSendOnSubscribe = 2
    }
}
