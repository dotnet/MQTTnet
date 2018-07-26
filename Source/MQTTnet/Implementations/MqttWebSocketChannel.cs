using System;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Channel;
using MQTTnet.Client;

#if NET452 || NET461
    using System.Net;
#endif

namespace MQTTnet.Implementations
{
    public class MqttWebSocketChannel : IMqttChannel
    {
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        private readonly MqttClientWebSocketOptions _options;

        private WebSocket _webSocket;

        public MqttWebSocketChannel(MqttClientWebSocketOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public MqttWebSocketChannel(WebSocket webSocket, string endpoint)
        {
            _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
            Endpoint = endpoint;
        }

        public string Endpoint { get; }

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

#if NET452 || NET461
            // use proxy if we have an address
            if (string.IsNullOrEmpty(_options.MqttClientWebSocketProxy.Address) == false)
            {
                var proxyUri = new Uri(_options.MqttClientWebSocketProxy.Address);

                // use proxy credentials if we have them
                if (string.IsNullOrEmpty(_options.MqttClientWebSocketProxy.Username) == false && string.IsNullOrEmpty(_options.MqttClientWebSocketProxy.Password) == false)
                {
                    var credentials = new NetworkCredential(_options.MqttClientWebSocketProxy.Username, _options.MqttClientWebSocketProxy.Password, _options.MqttClientWebSocketProxy.Domain);
                    clientWebSocket.Options.Proxy = new WebProxy(proxyUri, _options.MqttClientWebSocketProxy.BypassOnLocal, _options.MqttClientWebSocketProxy.BypassList, credentials);
                }
                else
                {
                    // use proxy without credentials
                    clientWebSocket.Options.Proxy = new WebProxy(proxyUri, _options.MqttClientWebSocketProxy.BypassOnLocal, _options.MqttClientWebSocketProxy.BypassList);
                }
            }
#endif

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
            var response = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer, offset, count), cancellationToken).ConfigureAwait(false);
            return response.Count;
        }

        public async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // This lock is required because the client will throw an exception if _SendAsync_ is 
            // called from multiple threads at the same time. But this issue only happens with several
            // framework versions.
            await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await _webSocket.SendAsync(new ArraySegment<byte>(buffer, offset, count), WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public void Dispose()
        {
            _sendLock?.Dispose();

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
    }
}