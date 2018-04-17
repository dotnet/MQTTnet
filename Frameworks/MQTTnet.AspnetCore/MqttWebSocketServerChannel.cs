using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Channel;
using MQTTnet.Implementations;

namespace MQTTnet.AspNetCore
{
    public class MqttWebSocketServerChannel : IMqttChannel
    {
        private WebSocket _webSocket;
        private readonly MqttWebSocketChannel _channel;

        public MqttWebSocketServerChannel(WebSocket webSocket)
        {
            _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));


            _channel = new MqttWebSocketChannel(webSocket);
            ReceiveStream = SendStream;
        }

        private Stream SendStream { get; set; }
        private Stream ReceiveStream { get; set; }

        public Task ConnectAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public async Task DisconnectAsync()
        {
            if (_webSocket == null)
            {
                return;
            }

            try
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                Dispose();
            }
        }

        public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return ReceiveStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return SendStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public void Dispose()
        {
            SendStream?.Dispose();
            ReceiveStream?.Dispose();

            _webSocket?.Dispose();

            SendStream = null;
            ReceiveStream = null;
            _webSocket = null;
        }
    }
}