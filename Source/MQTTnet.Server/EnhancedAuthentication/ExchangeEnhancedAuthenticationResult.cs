using MQTTnet.Packets;

namespace MQTTnet.Server.EnhancedAuthentication;

public sealed class ExchangeEnhancedAuthenticationResult
{
    public string ReasonString { get; set; }

    public List<MqttUserProperty> UserProperties { get; set; }

    public byte[] AuthenticationData { get; set; }
}