using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Implementations;
using MQTTnet.Serializer;
using MQTTnet.Server;

namespace MQTTnet.AspNetCore
{
    public class MqttWebSocketServerAdapter : IMqttServerAdapter
    {
        public event EventHandler<MqttServerAdapterClientAcceptedEventArgs> ClientAccepted;

        public Task StartAsync(IMqttServerOptions options)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            return Task.CompletedTask;
        }

        public Task AcceptWebSocketAsync(WebSocket webSocket)
        {
            if (webSocket == null) throw new ArgumentNullException(nameof(webSocket));

            var clientAdapter = new MqttChannelAdapter(new MqttWebSocketChannel(webSocket), new MqttPacketSerializer(), new MqttNetLogger().CreateChildLogger(nameof(MqttWebSocketServerAdapter)));

            var eventArgs = new MqttServerAdapterClientAcceptedEventArgs(clientAdapter);
            ClientAccepted?.Invoke(this, eventArgs);
            return eventArgs.SessionTask ?? Task.CompletedTask;
        }
        
        public void Dispose()
        {
            StopAsync().GetAwaiter().GetResult();
        }
    }
}