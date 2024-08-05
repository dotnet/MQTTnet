// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Channel;
using MQTTnet.Internal;

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

        public X509Certificate2 ClientCertificate { get; }

        public string Endpoint { get; }

        public bool IsSecureConnection { get; private set; }

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            var uri = _options.Uri;
            if (!uri.StartsWith("ws://", StringComparison.OrdinalIgnoreCase) && !uri.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
            {
                if (_options.TlsOptions?.UseTls == true)
                {
                    uri = "wss://" + uri;
                }
                else
                {
                    uri = "ws://" + uri;
                }
            }

            var clientWebSocket = new ClientWebSocket();
            try
            {
                SetupClientWebSocket(clientWebSocket);

                await clientWebSocket.ConnectAsync(new Uri(uri), cancellationToken).ConfigureAwait(false);
            }
            catch
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

        public void Dispose()
        {
            Cleanup();
        }

        public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var response = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer, offset, count), cancellationToken).ConfigureAwait(false);
            return response.Count;
        }

        public async Task WriteAsync(ReadOnlySequence<byte> buffer, bool isEndOfPacket, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // MQTT Control Packets MUST be sent in WebSocket binary data frames. If any other type of data frame is received the recipient MUST close the Network Connection [MQTT-6.0.0-1].
            // A single WebSocket data frame can contain multiple or partial MQTT Control Packets. The receiver MUST NOT assume that MQTT Control Packets are aligned on WebSocket frame boundaries [MQTT-6.0.0-2].
            long length = buffer.Length;
            foreach (var segment in buffer)
            {
                length -= segment.Length;
                bool endOfPacket = isEndOfPacket && length == 0;
                await _webSocket.SendAsync(segment, WebSocketMessageType.Binary, endOfPacket, cancellationToken).ConfigureAwait(false);
            }
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

            var proxyUri = new Uri(_options.ProxyOptions.Address);
            WebProxy webProxy;

            if (!string.IsNullOrEmpty(_options.ProxyOptions.Username) && !string.IsNullOrEmpty(_options.ProxyOptions.Password))
            {
                var credentials = new NetworkCredential(_options.ProxyOptions.Username, _options.ProxyOptions.Password, _options.ProxyOptions.Domain);
                webProxy = new WebProxy(proxyUri, _options.ProxyOptions.BypassOnLocal, _options.ProxyOptions.BypassList, credentials);
            }
            else
            {
                webProxy = new WebProxy(proxyUri, _options.ProxyOptions.BypassOnLocal, _options.ProxyOptions.BypassList);
            }

            if (_options.ProxyOptions.UseDefaultCredentials)
            {
                // Only update the property if required because setting it to false will alter
                // the used credentials internally!
                webProxy.UseDefaultCredentials = true;
            }

            return webProxy;
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

            if (_options.TlsOptions?.UseTls == true)
            {
                var certificates = _options.TlsOptions?.ClientCertificatesProvider?.GetCertificates();
                if (certificates?.Count > 0)
                {
                    clientWebSocket.Options.ClientCertificates = certificates;
                }
            }

            // Only set the value if it is actually true. This property is not supported on all platforms
            // and will throw a _PlatformNotSupported_ (i.e. WASM) exception when being used regardless of the actual value.
            if (_options.UseDefaultCredentials)
            {
                clientWebSocket.Options.UseDefaultCredentials = _options.UseDefaultCredentials;
            }

            if (_options.KeepAliveInterval != WebSocket.DefaultKeepAliveInterval)
            {
                clientWebSocket.Options.KeepAliveInterval = _options.KeepAliveInterval;
            }

            if (_options.Credentials != null)
            {
                clientWebSocket.Options.Credentials = _options.Credentials;
            }

            var certificateValidationHandler = _options.TlsOptions?.CertificateValidationHandler;
            if (certificateValidationHandler != null)
            {
                clientWebSocket.Options.RemoteCertificateValidationCallback = (_, certificate, chain, sslPolicyErrors) =>
                {
                    // TODO: Find a way to add client options to same callback. Problem is that they have a different type.
                    var context = new MqttClientCertificateValidationEventArgs(certificate, chain, sslPolicyErrors, _options);
                    return certificateValidationHandler(context);
                };

                var certificateSelectionHandler = _options.TlsOptions?.CertificateSelectionHandler;
                if (certificateSelectionHandler != null)
                {
                    throw new NotSupportedException("Remote certificate selection callback is not supported for WebSocket connections.");
                }
            }
        }
    }
}
