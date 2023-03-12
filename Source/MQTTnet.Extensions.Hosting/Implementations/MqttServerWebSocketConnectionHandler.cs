using Microsoft.Extensions.Hosting;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Formatter;
using MQTTnet.Implementations;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Extensions.Hosting.Implementations
{
    public class MqttServerWebSocketConnectionHandler : IHostedService, IDisposable
    {
        readonly CancellationTokenSource _cts = new CancellationTokenSource();
        readonly MqttWebSocketServerAdapter _adapter;
        readonly IMqttNetLogger _logger;

        public MqttServerWebSocketConnectionHandler(MqttWebSocketServerAdapter adapter, IMqttNetLogger logger)
        {
            _adapter = adapter;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.Cancel();

            return Task.CompletedTask;
        }

        public void HandleWebSocketConnection(HttpListenerWebSocketContext webSocketContext, HttpListenerContext httpListenerContext, X509Certificate2 clientCertificate = null)
        {
            _ = Task.Factory.StartNew(() => TryHandleWebSocketConnectionAsync(webSocketContext, httpListenerContext, clientCertificate));
        }

        async Task TryHandleWebSocketConnectionAsync(HttpListenerWebSocketContext webSocketContext, HttpListenerContext httpListenerContext, X509Certificate2 clientCertificate)
        {
            if (webSocketContext == null) throw new ArgumentNullException(nameof(webSocketContext));
            var endpoint = $"{httpListenerContext.Request.RemoteEndPoint.Address}:{httpListenerContext.Request.RemoteEndPoint.Port}";

            try
            {
                var clientHandler = _adapter.ClientHandler;
                if (clientHandler != null)
                {
                    var formatter = new MqttPacketFormatterAdapter(new MqttBufferWriter(4096, 65535));
                    var channel = new MqttWebSocketChannel(webSocketContext.WebSocket, endpoint, webSocketContext.IsSecureConnection, clientCertificate);
                    using (var channelAdapter = new MqttChannelAdapter(channel, formatter, null, _logger))
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

        public void Dispose()
        {
            _cts.Dispose();
        }
    }
}
