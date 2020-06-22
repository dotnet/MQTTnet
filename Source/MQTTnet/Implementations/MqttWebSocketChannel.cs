using MQTTnet.Channel;
using MQTTnet.Client.Options;
using MQTTnet.Internal;
using System;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Implementations
{
    public sealed class MqttWebSocketChannel : IMqttChannel
    {
        readonly MqttClientWebSocketOptions _options;

        AsyncLock _sendLock = new AsyncLock();
        WebSocket _webSocket;

        public MqttWebSocketChannel(MqttClientWebSocketOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public MqttWebSocketChannel(WebSocket webSocket, string endpoint, bool isSecureConnection, X509Certificate2 clientCertificate)
        {
            _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));

            Endpoint = endpoint;
            IsSecureConnection = isSecureConnection;
            ClientCertificate = clientCertificate;
        }

        public string Endpoint { get; }

        public bool IsSecureConnection { get; private set; }

        public X509Certificate2 ClientCertificate { get; }

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
            try
            {
                SetupClientWebSocket(clientWebSocket);

                await clientWebSocket.ConnectAsync(new Uri(uri), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Prevent a memory leak when always creating new instance which will fail while connecting.
                clientWebSocket.Dispose();
                throw;
            }

            _webSocket = clientWebSocket;
            IsSecureConnection = uri.StartsWith("wss://", StringComparison.OrdinalIgnoreCase);
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken)
        {
            if (_webSocket == null)
            {
                return;
            }

            if (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.Connecting)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken).ConfigureAwait(false);
            }

            Cleanup();
        }

        public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var response = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer, offset, count), cancellationToken).ConfigureAwait(false);
            return response.Count;
        }

        public async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // The lock is required because the client will throw an exception if _SendAsync_ is 
            // called from multiple threads at the same time. But this issue only happens with several
            // framework versions.
            if (_sendLock == null)
            {
                return;
            }

            using (await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                await _webSocket.SendAsync(new ArraySegment<byte>(buffer, offset, count), WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            Cleanup();
        }

        void SetupClientWebSocket(ClientWebSocket clientWebSocket)
        {

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
#if WINDOWS_UWP
                    clientWebSocket.Options.ClientCertificates.Add(new X509Certificate(certificate));
#else
                    clientWebSocket.Options.ClientCertificates.Add(certificate);
#endif

                }
            }

            var certificateValidationHandler = _options.TlsOptions?.CertificateValidationHandler;
#if NETSTANDARD2_1
            if (certificateValidationHandler != null)
            {
                clientWebSocket.Options.RemoteCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback((sender, certificate, chain, sslPolicyErrors) =>
                {
                    // TODO: Find a way to add client options to same callback. Problem is that they have a different type.
                    var context = new MqttClientCertificateValidationCallbackContext
                    {
                        Certificate = certificate,
                        Chain = chain,
                        SslPolicyErrors = sslPolicyErrors,
                        ClientOptions = _options
                    };

                    return certificateValidationHandler(context);
                });
            }
#else
            if (certificateValidationHandler != null)
            {
                throw new NotSupportedException("The remote certificate validation callback for Web Sockets is only supported for netstandard 2.1+");
            }
#endif
        }

        void Cleanup()
        {
            _sendLock?.Dispose();
            _sendLock = null;

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

        IWebProxy CreateProxy()
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
                var credentials = new NetworkCredential(_options.ProxyOptions.Username, _options.ProxyOptions.Password, _options.ProxyOptions.Domain);
                return new WebProxy(proxyUri, _options.ProxyOptions.BypassOnLocal, _options.ProxyOptions.BypassList, credentials);
            }

            return new WebProxy(proxyUri, _options.ProxyOptions.BypassOnLocal, _options.ProxyOptions.BypassList);
#endif
        }
    }
}