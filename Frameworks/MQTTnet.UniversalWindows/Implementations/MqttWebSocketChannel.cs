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
        private const int BufferSize = 4096;
        private const int BufferAmplifier = 20;
        private readonly byte[] WebSocketBuffer = new byte[BufferSize * BufferAmplifier];
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

        public Task ReadAsync(byte[] buffer)
        {
            return Task.WhenAll(ReadToBufferAsync(buffer));
        }

        private async Task ReadToBufferAsync(byte[] buffer)
        {
            var temporaryBuffer = new byte[BufferSize];
            var offset = 0;

            while (_webSocket.State == WebSocketState.Open)
            {
                if (WebSocketBufferSize == 0)
                {
                    WebSocketBufferOffset = 0;

                    WebSocketReceiveResult response;
                    do
                    {
                        response = await _webSocket.ReceiveAsync(new ArraySegment<byte>(temporaryBuffer), CancellationToken.None);

                        temporaryBuffer.CopyTo(WebSocketBuffer, offset);
                        offset += response.Count;
                        temporaryBuffer = new byte[BufferSize];
                    } while (!response.EndOfMessage);

                    WebSocketBufferSize = response.Count;
                    if (response.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    }

                    Buffer.BlockCopy(WebSocketBuffer, 0, buffer, 0, buffer.Length);
                    WebSocketBufferSize -= buffer.Length;
                    WebSocketBufferOffset += buffer.Length;
                }
                else
                {
                    Buffer.BlockCopy(WebSocketBuffer, WebSocketBufferOffset, buffer, 0, buffer.Length);
                    WebSocketBufferSize -= buffer.Length;
                    WebSocketBufferOffset += buffer.Length;
                }

                return;
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
    }
}