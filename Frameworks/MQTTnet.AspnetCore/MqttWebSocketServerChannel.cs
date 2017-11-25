using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Channel;
using MQTTnet.Implementations;

namespace MQTTnet.AspNetCore
{
    public class MqttWebSocketServerChannel : IMqttChannel, IDisposable
    {
        private WebSocket _webSocket;

        public MqttWebSocketServerChannel(WebSocket webSocket)
        {
            _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));

            SendStream = new WebSocketStream(_webSocket);
            ReceiveStream = SendStream;
        }

        public Stream SendStream { get; private set; }
        public Stream ReceiveStream { get; private set; }

        public Task ConnectAsync()
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
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            }
            finally
            {
                Dispose();
            }
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