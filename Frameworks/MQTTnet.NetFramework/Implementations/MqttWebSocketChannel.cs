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

        public Stream RawStream { get; private set; }
        public Stream SendStream => RawStream;
        public Stream ReceiveStream => RawStream;

        public async Task ConnectAsync(MqttClientOptions options)
        {
            _webSocket = new ClientWebSocket();
            await _webSocket.ConnectAsync(new Uri(options.Server), CancellationToken.None).ConfigureAwait(false);
            RawStream = new WebSocketStream(_webSocket);
        }

        public async Task DisconnectAsync()
        {
            RawStream = null;

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