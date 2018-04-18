using System;
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
        private readonly MqttClientWebSocketOptions _options;
        private readonly byte[] _chunkBuffer;

        private int _chunkBufferLength;
        private int _chunkBufferOffset;

        private WebSocket _webSocket;

        public MqttWebSocketChannel(MqttClientWebSocketOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            _chunkBuffer = new byte[options.BufferSize];
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

            while (_webSocket.State == WebSocketState.Open)
            {
                await EnsureFilledChunkBufferAsync(cancellationToken).ConfigureAwait(false);
                if (_chunkBufferLength == 0)
                {
                    return 0;
                }

                while (count > 0 && _chunkBufferOffset < _chunkBufferLength)
                {
                    buffer[offset] = _chunkBuffer[_chunkBufferOffset];
                    _chunkBufferOffset++;
                    count--;
                    bytesRead++;
                    offset++;
                }

                if (count == 0)
                {
                    return bytesRead;
                }
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

        private async Task EnsureFilledChunkBufferAsync(CancellationToken cancellationToken)
        {
            if (_chunkBufferOffset < _chunkBufferLength)
            {
                return;
            }

            if (_webSocket.State == WebSocketState.Closed)
            {
                throw new MqttCommunicationException("WebSocket connection closed.");
            }

            var response = await _webSocket.ReceiveAsync(new ArraySegment<byte>(_chunkBuffer, 0, _chunkBuffer.Length), cancellationToken).ConfigureAwait(false);
            _chunkBufferLength = response.Count;
            _chunkBufferOffset = 0;

            if (response.MessageType == WebSocketMessageType.Close)
            {
                throw new MqttCommunicationException("The WebSocket server closed the connection.");
                //await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken).ConfigureAwait(false);
            }

            if (response.MessageType == WebSocketMessageType.Text)
            {
                throw new MqttProtocolViolationException("WebSocket channel received TEXT message.");
            }
        }
    }
}