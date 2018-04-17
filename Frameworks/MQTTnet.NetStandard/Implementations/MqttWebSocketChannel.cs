using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Channel;
using MQTTnet.Client;
using MQTTnet.Exceptions;

namespace MQTTnet.Implementations
{
    public sealed class MqttWebSocketChannel : IMqttChannel
    {
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        public static int BufferSize { get; set; } = 4096; // Can be changed for fine tuning by library user.

        private readonly byte[] _chunckBuffer = new byte[BufferSize];
        private readonly Queue<byte> _buffer = new Queue<byte>(BufferSize);
        private readonly MqttClientWebSocketOptions _options;

        private WebSocket _webSocket;

        public MqttWebSocketChannel(MqttClientWebSocketOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public MqttWebSocketChannel(WebSocket webSocket)
        {
            _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
        }

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            var uri = _options.Uri;
            if (!uri.StartsWith("ws://", StringComparison.OrdinalIgnoreCase) && !uri.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
            {
                if (_options.TlsOptions?.UseTls == false)
                {
                    uri = "ws://" + uri;
                }
                else
                {
                    uri = "wss://" + uri;
                }
            }

            var clientWebSocket = new ClientWebSocket();
            
            if (_options.RequestHeaders != null)
            {
                foreach (var requestHeader in _options.RequestHeaders)
                {
                    clientWebSocket.Options.SetRequestHeader(requestHeader.Key, requestHeader.Value);
                }    
            }

            if (_options.SubProtocols != null)
            {
                foreach (var subProtocol in _options.SubProtocols)
                {
                    clientWebSocket.Options.AddSubProtocol(subProtocol);
                }
            }

            if (_options.CookieContainer != null)
            {
                clientWebSocket.Options.Cookies = _options.CookieContainer;
            }

            if (_options.TlsOptions?.UseTls == true && _options.TlsOptions?.Certificates != null)
            {
                clientWebSocket.Options.ClientCertificates = new X509CertificateCollection();
                foreach (var certificate in _options.TlsOptions.Certificates)
                {
                    clientWebSocket.Options.ClientCertificates.Add(new X509Certificate(certificate));
                }
            }

            await clientWebSocket.ConnectAsync(new Uri(uri), cancellationToken).ConfigureAwait(false);
            _webSocket = clientWebSocket;
        }

        public async Task DisconnectAsync()
        {
            if (_webSocket == null)
            {
                return;
            }

            if (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.Connecting)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None).ConfigureAwait(false);
            }

            Dispose();
        }

        public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var bytesRead = 0;

            // Use existing date from buffer.
            while (count > 0 && _buffer.Any())
            {
                buffer[offset] = _buffer.Dequeue();
                count--;
                bytesRead++;
                offset++;
            }

            if (count == 0)
            {
                return bytesRead;
            }

            // Fetch new data if the buffer is not full.
            while (_webSocket.State == WebSocketState.Open)
            {
                await FetchChunkAsync(cancellationToken).ConfigureAwait(false);

                while (count > 0 && _buffer.Any())
                {
                    buffer[offset] = _buffer.Dequeue();
                    count--;
                    bytesRead++;
                    offset++;
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

        public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _webSocket.SendAsync(new ArraySegment<byte>(buffer, offset, count), WebSocketMessageType.Binary, true, cancellationToken);
        }

        public void Dispose()
        {
            try
            {
                _webSocket?.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                _webSocket = null;
            }            
        }

        private async Task FetchChunkAsync(CancellationToken cancellationToken)
        {
            var response = await _webSocket.ReceiveAsync(new ArraySegment<byte>(_chunckBuffer, 0, _chunckBuffer.Length), cancellationToken).ConfigureAwait(false);

            if (response.MessageType == WebSocketMessageType.Close)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken).ConfigureAwait(false);
            }
            else if (response.MessageType == WebSocketMessageType.Binary)
            {
                for (var i = 0; i < response.Count; i++)
                {
                    _buffer.Enqueue(_chunckBuffer[i]);
                }
            }
            else if (response.MessageType == WebSocketMessageType.Text)
            {
                throw new MqttProtocolViolationException("WebSocket channel received TEXT message.");
            }
        }
    }
}