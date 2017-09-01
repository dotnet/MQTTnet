using MQTTnet.Core.Channel;
using MQTTnet.Core.Client;
using MQTTnet.Core.Exceptions;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Implementations
{
    public sealed class MqttWebSocketsChannel : IMqttCommunicationChannel, IDisposable
    {
        private ClientWebSocket _webSocket = null;
        private const int BufferSize = 4096;
        private const int BufferAmplifier = 20;
        private byte[] WebSocketBuffer = new byte[BufferSize * BufferAmplifier];
        private int WebSocketBufferSize = 0;
        private int WebSocketBufferOffset = 0;

        public MqttWebSocketsChannel()
        {
            _webSocket = new ClientWebSocket();
        }

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
            finally
            {
            }
        }

        public async Task DisconnectAsync()
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
        }

        public void Dispose()
        {
            if (_webSocket != null)
                _webSocket.Dispose();
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
                        response =
                            await _webSocket.ReceiveAsync(new ArraySegment<byte>(temporaryBuffer), CancellationToken.None);

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

        public Task WriteAsync(byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            var writeBuffer = System.Text.Encoding.ASCII.GetString(buffer);
            try
            {
                return _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Binary, true,
                 CancellationToken.None);
            }
            catch (WebSocketException exception)
            {
                throw new MqttCommunicationException(exception);
            }
        }
    }
}