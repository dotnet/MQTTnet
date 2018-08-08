namespace MQTTnet.Protocol
{
    public enum MqttConnectReturnCode
    {
        ConnectionAccepted = 0x00,
        ConnectionRefusedUnacceptableProtocolVersion = 0x01,
        ConnectionRefusedIdentifierRejected = 0x02,
        ConnectionRefusedServerUnavailable = 0x03,
        ConnectionRefusedBadUsernameOrPassword = 0x04,
        ConnectionRefusedNotAuthorized = 0x05
    }
}
