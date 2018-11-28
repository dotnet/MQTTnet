namespace MQTTnet.Protocol
{
    public enum MqttSubscribeReasonCode
    {
        GrantedQoS0 = 0,
        GrantedQoS1 = 1,
        GrantedQoS2 = 2,
        UnspecifiedError = 128,
        ImplementationSpecificError = 131,
        NotAuthorized = 135,
        TopicFilterInvalid = 143,
        PacketIdentifierInUse = 145,
        QuotaExceeded = 151,
        SharedSubscriptionsNotSupported = 158,
        SubscriptionIdentifiersNotSupported = 161,
        WildcardSubscriptionsNotSupported = 162
    }
}
