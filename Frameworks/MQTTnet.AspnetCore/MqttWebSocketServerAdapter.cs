using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Serializer;
using MQTTnet.Server;

namespace MQTTnet.AspNetCore
{
    public class MqttWebSocketServerAdapter : IMqttServerAdapter, IDisposable
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

            var channel = new MqttWebSocketServerChannel(webSocket);
            var clientAdapter = new MqttChannelAdapter(channel, new MqttPacketSerializer(), new MqttNetLogger());

            var eventArgs = new MqttServerAdapterClientAcceptedEventArgs(clientAdapter);
            ClientAccepted?.Invoke(this, eventArgs);
            return eventArgs.SessionTask;
        }
        
        public void Dispose()
        {
            StopAsync();
        }
    }
}