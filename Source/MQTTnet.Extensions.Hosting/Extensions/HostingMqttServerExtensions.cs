using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Extensions.Hosting;
using MQTTnet.Extensions.Hosting.Implementations;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace MQTTnet.Server
{
    public static class HostingMqttServerExtensions
    {

        public static void HandleWebSocketConnection(this MqttServer server, HttpListenerWebSocketContext webSocketContext, HttpListenerContext httpListenerContext)
        {
            if (!(server is MqttHostedServer mqttHostedServer))
                throw new InvalidOperationException("The server must be started through hosting extensions.");

            mqttHostedServer.ServiceProvider.GetRequiredService<MqttServerWebSocketConnectionHandler>().HandleWebSocketConnection(webSocketContext, httpListenerContext);
        }

    }
}
