using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Channel;
using MQTTnet.Core.Serializer;
using MQTTnet.Core.Server;
using MQTTnet.Implementations;
using Microsoft.Extensions.Logging;

namespace MQTTnet.TestApp.AspNetCore2
{
    public class MqttWebSocketServerAdapter : IMqttServerAdapter, IDisposable
    {
        private readonly ILogger<MqttWebSocketServerAdapter> _logger;
        private readonly IMqttCommunicationAdapterFactory _mqttCommunicationAdapterFactory;

        public MqttWebSocketServerAdapter(ILogger<MqttWebSocketServerAdapter> logger, IMqttCommunicationAdapterFactory mqttCommunicationAdapterFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mqttCommunicationAdapterFactory = mqttCommunicationAdapterFactory ?? throw new ArgumentNullException(nameof(mqttCommunicationAdapterFactory));
        }

        public event EventHandler<MqttServerAdapterClientAcceptedEventArgs> ClientAccepted;

        public Task StartAsync(MqttServerOptions options)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            return Task.FromResult(0);
        }

        public Task AcceptWebSocketAsync(WebSocket webSocket)
        {
            if (webSocket == null) throw new ArgumentNullException(nameof(webSocket));

            var channel = new MqttWebSocketServerChannel(webSocket);
            var clientAdapter = _mqttCommunicationAdapterFactory.CreateServerMqttCommunicationAdapter(channel);

            var eventArgs = new MqttServerAdapterClientAcceptedEventArgs(clientAdapter);
            ClientAccepted?.Invoke(this, eventArgs);
            return eventArgs.SessionTask;
        }
        
        public void Dispose()
        {
            StopAsync();
        }

        private class MqttWebSocketServerChannel : IMqttCommunicationChannel, IDisposable
        {
            private readonly WebSocket _webSocket;

            public MqttWebSocketServerChannel(WebSocket webSocket)
            {
                _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));

                RawReceiveStream = new WebSocketStream(_webSocket);
            }

            public Stream SendStream => RawReceiveStream;
            public Stream ReceiveStream => RawReceiveStream;
            public Stream RawReceiveStream { get; }

            public Task ConnectAsync()
            {
                return Task.CompletedTask;
            }

            public Task DisconnectAsync()
            {
                RawReceiveStream?.Dispose();

                if (_webSocket == null)
                {
                    return Task.CompletedTask;
                }

                return _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            }

            public void Dispose()
            {
                RawReceiveStream?.Dispose();
                SendStream?.Dispose();
                ReceiveStream?.Dispose();

                _webSocket?.Dispose();
            }
        }
    }
}