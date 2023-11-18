using System;
using System.Net;
using System.Net.WebSockets;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Extensions.Hosting.Implementations;
using MQTTnet.Server;

namespace MQTTnet.Extensions.Hosting.Extensions
{
    public static class HostingMqttServerExtensions
    {
        public static void HandleWebSocketConnection(this MqttServer server, HttpListenerWebSocketContext webSocketContext, HttpListenerContext httpListenerContext)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            if (!(server is MqttHostedServer mqttHostedServer))
            {
                throw new InvalidOperationException("The server must be started through hosting extensions.");
            }

            mqttHostedServer.ServiceProvider.GetRequiredService<MqttServerWebSocketConnectionHandler>().HandleWebSocketConnection(webSocketContext, httpListenerContext);
        }
    }
}