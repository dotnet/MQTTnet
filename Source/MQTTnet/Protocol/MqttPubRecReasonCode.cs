namespace MQTTnet.Protocol
{
    public enum MqttPubRecReasonCode
    {
        Success = 0,
        NoMatchingSubscribers = 16,
        UnspecifiedError = 128,
        ImplementationSpecificError = 131,
        NotAuthorized = 135,
        TopicNameInvalid = 144,
        PacketIdentifierInUse = 145,
        QuotaExceeded = 151,
        PayloadFormatInvalid = 153
    }
}
