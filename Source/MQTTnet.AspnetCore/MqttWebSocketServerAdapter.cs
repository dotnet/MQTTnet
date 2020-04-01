using Microsoft.AspNetCore.Http;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Formatter;
using MQTTnet.Implementations;
using MQTTnet.Server;
using System;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore
{
    public class MqttWebSocketServerAdapter : IMqttServerAdapter
    {
        private readonly IMqttNetLogger _logger;

        public MqttWebSocketServerAdapter(IMqttNetLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            _logger = logger.CreateChildLogger(nameof(MqttTcpServerAdapter));
        }

        public Func<IMqttChannelAdapter, Task> ClientHandler { get; set; }

        public Task StartAsync(IMqttServerOptions options)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            return Task.CompletedTask;
        }

        public async Task RunWebSocketConnectionAsync(WebSocket webSocket, HttpContext httpContext)
        {
            if (webSocket == null) throw new ArgumentNullException(nameof(webSocket));

            var endpoint = $"{httpContext.Connection.RemoteIpAddress}:{httpContext.Connection.RemotePort}";

            var clientCertificate = await httpContext.Connection.GetClientCertificateAsync().ConfigureAwait(false);
            try
            {
                var isSecureConnection = clientCertificate != null;

                var clientHandler = ClientHandler;
                if (clientHandler != null)
                {
                    var writer = new SpanBasedMqttPacketWriter();
                    var formatter = new MqttPacketFormatterAdapter(writer);
                    var channel = new MqttWebSocketChannel(webSocket, endpoint, isSecureConnection, clientCertificate);

                    using (var channelAdapter = new MqttChannelAdapter(channel, formatter, _logger.CreateChildLogger(nameof(MqttWebSocketServerAdapter))))
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
        }
    }
}