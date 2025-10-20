using MQTTnet.Packets;

namespace MQTTnet.Server.EnhancedAuthentication;

public sealed class ExchangeEnhancedAuthenticationResult
{
    public string? ReasonString { get; init; }

    public List<MqttUserProperty>? UserProperties { get; init; }

    public byte[]? AuthenticationData { get; init; }
}