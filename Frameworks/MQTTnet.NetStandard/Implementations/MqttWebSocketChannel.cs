using MQTTnet.Core.Channel;
using MQTTnet.Core.Client;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Implementations
{
    public sealed class MqttWebSocketChannel : IMqttCommunicationChannel, IDisposable
    {
        private readonly MqttClientWebSocketOptions _options;
        private ClientWebSocket _webSocket;

        public MqttWebSocketChannel(MqttClientWebSocketOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public Stream SendStream => RawReceiveStream;
        public Stream ReceiveStream => RawReceiveStream;
        public Stream RawReceiveStream { get; private set; }

        public async Task ConnectAsync()
        {
            var uri = _options.Uri;
            if (!uri.StartsWith("ws://", StringComparison.OrdinalIgnoreCase))
            {
                uri = "ws://" + uri;
            }

            _webSocket = new ClientWebSocket();

            if (_options.RequestHeaders != null)
            {
                foreach (var requestHeader in _options.RequestHeaders)
                {
                    _webSocket.Options.SetRequestHeader(requestHeader.Key, requestHeader.Value);
                }    
            }

            if (_options.SubProtocols != null)
            {
                foreach (var subProtocol in _options.SubProtocols)
                {
                    _webSocket.Options.AddSubProtocol(subProtocol);
                }
            }

            if (_options.CookieContainer != null)
            {
                _webSocket.Options.Cookies = _options.CookieContainer;
            }

            if (_options.TlsOptions?.UseTls == true && _options.TlsOptions?.Certificates != null)
            {
                _webSocket.Options.ClientCertificates = new X509CertificateCollection();
                foreach (var certificate in _options.TlsOptions.Certificates)
                {
                    _webSocket.Options.ClientCertificates.Add(new X509Certificate(certificate));
                }
            }

            await _webSocket.ConnectAsync(new Uri(uri), CancellationToken.None);
            RawReceiveStream = new WebSocketStream(_webSocket);
        }

        public async Task DisconnectAsync()
        {
            RawReceiveStream = null;

            if (_webSocket == null)
            {
                return;
            }

            if (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.Connecting)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            _webSocket?.Dispose();
            _webSocket = null;
        }
    }
}