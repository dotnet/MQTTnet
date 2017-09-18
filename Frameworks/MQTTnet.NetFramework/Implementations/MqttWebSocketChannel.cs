using MQTTnet.Core.Channel;
using MQTTnet.Core.Client;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Implementations
{
    public sealed class MqttWebSocketChannel : IMqttCommunicationChannel, IDisposable
    {
        private ClientWebSocket _webSocket;

        public Stream RawReceiveStream { get; private set; }
        public Stream SendStream => RawReceiveStream;
        public Stream ReceiveStream => RawReceiveStream;

        public async Task ConnectAsync(MqttClientOptions options)
        {
            var uri = options.Server;
            if (!uri.StartsWith("ws://", StringComparison.OrdinalIgnoreCase))
            {
                uri = "ws://" + uri;
            }

            if (options.Port.HasValue)
            {
                uri += ":" + options.Port;
            }
            
            _webSocket = new ClientWebSocket();
            _webSocket.Options.KeepAliveInterval = options.KeepAlivePeriod;
            await _webSocket.ConnectAsync(new Uri(uri), CancellationToken.None).ConfigureAwait(false);
            RawReceiveStream = new WebSocketStream(_webSocket);
        }

        public async Task DisconnectAsync()
        {
            RawReceiveStream = null;

            if (_webSocket == null)
            {
                return;
            }

            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None).ConfigureAwait(false);
        }

        public void Dispose()
        {
            _webSocket?.Dispose();
        }
    }
}