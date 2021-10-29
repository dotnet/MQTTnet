namespace MQTTnet.Client
{
    public enum MqttApplicationMessageReceivedReasonCode
    {
        Success = 0,
        NoMatchingSubscribers = 16,
        UnspecifiedError = 128,
        ImplementationSpecificError = 131,
        NotAuthorized = 135,
        TopicNameInvalid = 144,
        PacketIdentifierInUse = 145,
        PacketIdentifierNotFound = 146,
        QuotaExceeded = 151,
        PayloadFormatInvalid = 153
    }
}