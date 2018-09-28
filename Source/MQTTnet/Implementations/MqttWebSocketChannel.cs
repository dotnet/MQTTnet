using System;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Channel;
using MQTTnet.Client;

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

            if (_options.ProxyOptions != null)
            {
                clientWebSocket.Options.Proxy = CreateProxy();
            }

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

        private IWebProxy CreateProxy()
        {
            if (string.IsNullOrEmpty(_options.ProxyOptions?.Address))
            {
                return null;
            }

#if WINDOWS_UWP
            throw new NotSupportedException("Proxies are not supported in UWP.");
#elif NETSTANDARD1_3
            throw new NotSupportedException("Proxies are not supported in netstandard 1.3.");
#else
            var proxyUri = new Uri(_options.ProxyOptions.Address);

            if (!string.IsNullOrEmpty(_options.ProxyOptions.Username) && !string.IsNullOrEmpty(_options.ProxyOptions.Password))
            {
                var credentials =
                    new NetworkCredential(_options.ProxyOptions.Username, _options.ProxyOptions.Password, _options.ProxyOptions.Domain);

                return new WebProxy(proxyUri, _options.ProxyOptions.BypassOnLocal, _options.ProxyOptions.BypassList, credentials);
            }

            return new WebProxy(proxyUri, _options.ProxyOptions.BypassOnLocal, _options.ProxyOptions.BypassList);
#endif
        }
    }
}