namespace MQTTnet.Client.Unsubscribing
{
    public enum MqttClientUnsubscribeResultCode
    {
        Success = 0,
        NoSubscriptionExisted = 17,
        UnspecifiedError = 128,
        ImplementationSpecificError = 131,
        NotAuthorized = 135,
        TopicFilterInvalid = 143,
        PacketIdentifierInUse = 145
    }
}
