using MQTTnet.Packets;

namespace MQTTnet.Server.EnhancedAuthentication;

public static class ExchangeEnhancedAuthenticationResultFactory
{
    public static ExchangeEnhancedAuthenticationResult Create(MqttAuthPacket authPacket)
    {
        ArgumentNullException.ThrowIfNull(authPacket);

        return new ExchangeEnhancedAuthenticationResult
        {
            AuthenticationData = authPacket.AuthenticationData,

            ReasonString = authPacket.ReasonString,
            UserProperties = authPacket.UserProperties
        };
    }

    public static ExchangeEnhancedAuthenticationResult Create(MqttDisconnectPacket disconnectPacket)
    {
        ArgumentNullException.ThrowIfNull(disconnectPacket);

        return new ExchangeEnhancedAuthenticationResult
        {
            AuthenticationData = null,
            ReasonString = disconnectPacket.ReasonString,
            UserProperties = disconnectPacket.UserProperties

            // SessionExpiryInterval makes no sense because the connection is not yet made!
            // ServerReferences makes no sense when the client initiated a DISCONNECT!
        };
    }
}