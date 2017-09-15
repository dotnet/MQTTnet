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
        private ClientWebSocket _webSocket = new ClientWebSocket();

        public Stream RawStream { get; private set; }
        public Stream SendStream => RawStream;
        public Stream ReceiveStream => RawStream;

        public async Task ConnectAsync(MqttClientOptions options)
        {
            _webSocket = new ClientWebSocket();
            await _webSocket.ConnectAsync(new Uri(options.Server), CancellationToken.None);
            RawStream = new WebSocketStream(_webSocket);
        }

        public Task DisconnectAsync()
        {
            RawStream = null;
            return _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
        }

        public void Dispose()
        {
            _webSocket?.Dispose();
        }
    }
}