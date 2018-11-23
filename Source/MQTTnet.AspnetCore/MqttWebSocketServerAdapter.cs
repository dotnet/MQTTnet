using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Formatter;
using MQTTnet.Implementations;
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

        public async Task RunWebSocketConnectionAsync(WebSocket webSocket, string endpoint)
        {
            if (webSocket == null) throw new ArgumentNullException(nameof(webSocket));

            var clientAdapter = new MqttChannelAdapter(new MqttWebSocketChannel(webSocket, endpoint), new MqttPacketFormatterAdapter(), new MqttNetLogger().CreateChildLogger(nameof(MqttWebSocketServerAdapter)));

            var eventArgs = new MqttServerAdapterClientAcceptedEventArgs(clientAdapter);
            ClientAccepted?.Invoke(this, eventArgs);

            if (eventArgs.SessionTask != null)
            {
                await eventArgs.SessionTask.ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            StopAsync().GetAwaiter().GetResult();
        }
    }
}