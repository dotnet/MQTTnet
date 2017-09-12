using MQTTnet.Core.Channel;
using MQTTnet.Core.Client;
using MQTTnet.Core.Exceptions;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Implementations
{
    public sealed class MqttWebSocketChannel : IMqttCommunicationChannel, IDisposable
    {
        private ClientWebSocket _webSocket = new ClientWebSocket();
        private int WebSocketBufferSize;
        private int WebSocketBufferOffset;

        public async Task ConnectAsync(MqttClientOptions options)
        {
            _webSocket = null;

            try
            {
                _webSocket = new ClientWebSocket();
                await _webSocket.ConnectAsync(new Uri(options.Server), CancellationToken.None);
            }
            catch (WebSocketException exception)
            {
                throw new MqttCommunicationException(exception);
            }
        }

        public Task DisconnectAsync()
        {
            return _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
        }

        public void Dispose()
        {
            _webSocket?.Dispose();
        }

        public async Task<ArraySegment<byte>> ReadAsync(int length, byte[] buffer)
        {
            await ReadToBufferAsync(length, buffer).ConfigureAwait(false);

            var result = new ArraySegment<byte>(buffer, WebSocketBufferOffset, length);
            WebSocketBufferSize -= length;
            WebSocketBufferOffset += length;

            return result;
        }

        private async Task ReadToBufferAsync(int length, byte[] buffer)
        {
            if (WebSocketBufferSize > 0)
            {
                return;
            }

            var offset = 0;
            while (_webSocket.State == WebSocketState.Open && WebSocketBufferSize < length)
            {
                WebSocketReceiveResult response;
                do
                {
                    response = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer, offset, buffer.Length - offset), CancellationToken.None).ConfigureAwait(false);
                    offset += response.Count;
                } while (!response.EndOfMessage);

                WebSocketBufferSize = response.Count;
                if (response.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None).ConfigureAwait(false);
                }
            }
        }

        public async Task WriteAsync(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            try
            {
                await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Binary, true, CancellationToken.None);
            }
            catch (WebSocketException exception)
            {
                throw new MqttCommunicationException(exception);
            }
        }

        public int Peek()
        {
            return WebSocketBufferSize;
        }
    }
}