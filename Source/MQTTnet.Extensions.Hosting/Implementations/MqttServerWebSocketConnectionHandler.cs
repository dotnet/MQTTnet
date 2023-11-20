using System;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Formatter;
using MQTTnet.Implementations;

namespace MQTTnet.Extensions.Hosting.Implementations
{
    public sealed class MqttServerWebSocketConnectionHandler : IHostedService, IDisposable
    {
        readonly MqttWebSocketServerAdapter _adapter;
        readonly CancellationTokenSource _cancellationToken = new CancellationTokenSource();
        readonly IMqttNetLogger _logger;

        public MqttServerWebSocketConnectionHandler(MqttWebSocketServerAdapter adapter, IMqttNetLogger logger)
        {
            _adapter = adapter;
            _logger = logger;
        }

        public void Dispose()
        {
            _cancellationToken.Dispose();
        }

        public void HandleWebSocketConnection(HttpListenerWebSocketContext webSocketContext, HttpListenerContext httpListenerContext, X509Certificate2? clientCertificate = null)
        {
            _ = Task.Factory.StartNew(() => TryHandleWebSocketConnectionAsync(webSocketContext, httpListenerContext, clientCertificate));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellationToken.Cancel();

            return Task.CompletedTask;
        }

        async Task TryHandleWebSocketConnectionAsync(HttpListenerWebSocketContext webSocketContext, HttpListenerContext httpListenerContext, X509Certificate2? clientCertificate)
        {
            if (webSocketContext == null)
            {
                throw new ArgumentNullException(nameof(webSocketContext));
            }

            var endpoint = $"{httpListenerContext.Request.RemoteEndPoint.Address}:{httpListenerContext.Request.RemoteEndPoint.Port}";

            try
            {
                var clientHandler = _adapter.ClientHandler;
                if (clientHandler != null)
                {
                    var formatter = new MqttPacketFormatterAdapter(new MqttBufferWriter(4096, 65535));
                    var channel = new MqttWebSocketChannel(webSocketContext.WebSocket, endpoint, webSocketContext.IsSecureConnection, clientCertificate);
                    using (var channelAdapter = new MqttChannelAdapter(channel, formatter, _logger))
                    {
                        await clientHandler(channelAdapter).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                clientCertificate?.Dispose();
            }
        }
    }
}