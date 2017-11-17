using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Server;

namespace MQTTnet.AspNetCore
{
    public class MqttWebSocketServerAdapter : IMqttServerAdapter, IDisposable
    {
        private readonly IMqttCommunicationAdapterFactory _mqttCommunicationAdapterFactory;

        public MqttWebSocketServerAdapter(IMqttCommunicationAdapterFactory mqttCommunicationAdapterFactory)
        {
            _mqttCommunicationAdapterFactory = mqttCommunicationAdapterFactory ?? throw new ArgumentNullException(nameof(mqttCommunicationAdapterFactory));
        }

        public event EventHandler<MqttServerAdapterClientAcceptedEventArgs> ClientAccepted;

        public Task StartAsync(MqttServerOptions options)
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
            var clientAdapter = _mqttCommunicationAdapterFactory.CreateServerCommunicationAdapter(channel);

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