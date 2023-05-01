using MQTTnet.Extensions.Hosting.Events;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Text;

namespace MQTTnet.Extensions.Hosting.Options
{
    public class MqttServerHostingOptions
    {
        public bool AutoRemoveEventHandlers { get; set; } = true;

        public MqttServerWebSocketEndpointOptions DefaultWebSocketEndpointOptions { get; } = new MqttServerWebSocketEndpointOptions();

        public MqttServerTlsWebSocketEndpointOptions DefaultTlsWebSocketEndpointOptions { get; } = new MqttServerTlsWebSocketEndpointOptions();

        public HttpWebSocketClientAuthenticationCallback WebSocketAuthenticationCallback { get; set; }

        public string WebSocketRoute { get; set; }

    }
}
