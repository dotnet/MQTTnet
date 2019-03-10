using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Formatter;
using MQTTnet.Implementations;
using MQTTnet.Server;

namespace MQTTnet.AspNetCore
{
    public class MqttWebSocketServerAdapter : IMqttServerAdapter
    {
        private readonly IMqttNetChildLogger _logger;

        public MqttWebSocketServerAdapter(IMqttNetChildLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            _logger = logger.CreateChildLogger(nameof(MqttTcpServerAdapter));
        }

        public Action<MqttServerAdapterClientAcceptedEventArgs> ClientAcceptedHandler { get; set; }

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
            var channel = new MqttWebSocketChannel(webSocket, endpoint);
            var clientAdapter = new MqttChannelAdapter(channel, new MqttPacketFormatterAdapter(), _logger.CreateChildLogger(nameof(MqttWebSocketServerAdapter)));

            var clientCertificate = await httpContext.Connection.GetClientCertificateAsync().ConfigureAwait(false);
            var isSecureConnection = clientCertificate != null;
            clientCertificate?.Dispose();

            var eventArgs = new MqttServerAdapterClientAcceptedEventArgs(clientAdapter);
            ClientAcceptedHandler?.Invoke(eventArgs);

            if (eventArgs.SessionTask != null)
            {
                await eventArgs.SessionTask.ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
        }
    }
}