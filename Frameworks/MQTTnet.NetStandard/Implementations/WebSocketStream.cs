using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Exceptions;

namespace MQTTnet.Implementations
{
    public class WebSocketStream : Stream
    {
        private readonly WebSocket _webSocket;
        private readonly byte[] _chunkBuffer = new byte[MqttWebSocketChannel.BufferSize];
        private readonly Queue<byte> _buffer = new Queue<byte>(MqttWebSocketChannel.BufferSize);

        public WebSocketStream(WebSocket webSocket)
        {
            _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count).GetAwaiter().GetResult();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var bytesRead = 0;

            // Use existing date from buffer.
            while (count > 0 && _buffer.Any())
            {
                buffer[offset++] = _buffer.Dequeue();
                count--;
                bytesRead++;
            }

            if (count == 0)
            {
                return bytesRead;
            }
            
            while (_webSocket.State == WebSocketState.Open)
            {
                await FetchChunkAsync(cancellationToken);

                while (count > 0 && _buffer.Any())
                {
                    buffer[offset++] = _buffer.Dequeue();
                    count--;
                    bytesRead++;
                }

                if (count == 0)
                {
                    return bytesRead;
                }
            }

            if (_webSocket.State == WebSocketState.Closed)
            {
                throw new MqttCommunicationException("WebSocket connection closed.");
            }

            return bytesRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteAsync(buffer, offset, count).GetAwaiter().GetResult();
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _webSocket.SendAsync(new ArraySegment<byte>(buffer, offset, count), WebSocketMessageType.Binary, true, cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        private async Task FetchChunkAsync(CancellationToken cancellationToken)
        {
            var response = await _webSocket.ReceiveAsync(new ArraySegment<byte>(_chunkBuffer, 0, _chunkBuffer.Length), cancellationToken).ConfigureAwait(false);

            for (var i = 0; i < response.Count; i++)
            {
                var @byte = _chunkBuffer[i];
                _buffer.Enqueue(@byte);
            }

            if (response.MessageType == WebSocketMessageType.Close)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
