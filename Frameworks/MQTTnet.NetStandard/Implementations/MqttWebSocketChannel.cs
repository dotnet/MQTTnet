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
    public sealed class MqttWebSocketChannel : IMqttChannel, IDisposable
    {
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        public static int BufferSize { get; set; } = 4096 * 20; // Can be changed for fine tuning by library user.

        private readonly MqttClientWebSocketOptions _options;
        private ClientWebSocket _webSocket;

        public MqttWebSocketChannel(MqttClientWebSocketOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public Stream SendStream { get; private set; }
        public Stream ReceiveStream { get; private set; }

        public async Task ConnectAsync()
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

            await _webSocket.ConnectAsync(new Uri(uri), CancellationToken.None).ConfigureAwait(false);

            SendStream = new WebSocketStream(_webSocket);
            ReceiveStream = SendStream;
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

        public void Dispose()
        {
            _webSocket?.Dispose();
            _webSocket = null;
        }
    }
}