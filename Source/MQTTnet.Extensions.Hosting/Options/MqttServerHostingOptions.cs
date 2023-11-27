using MQTTnet.Extensions.Hosting.Events;

namespace MQTTnet.Extensions.Hosting.Options
{
    public sealed class MqttServerHostingOptions
    {
        public bool AutoRemoveEventHandlers { get; set; } = true;

        public MqttServerTlsWebSocketEndpointOptions DefaultTlsWebSocketEndpointOptions { get; } = new MqttServerTlsWebSocketEndpointOptions();

        public MqttServerWebSocketEndpointOptions DefaultWebSocketEndpointOptions { get; } = new MqttServerWebSocketEndpointOptions();
    }
}