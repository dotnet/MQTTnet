// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Channel;
using MQTTnet.Client;
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

        public async Task WriteAsync(ArraySegment<byte> buffer, bool isEndOfPacket, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

#if NET5_0_OR_GREATER
            // MQTT Control Packets MUST be sent in WebSocket binary data frames. If any other type of data frame is received the recipient MUST close the Network Connection [MQTT-6.0.0-1].
            // A single WebSocket data frame can contain multiple or partial MQTT Control Packets. The receiver MUST NOT assume that MQTT Control Packets are aligned on WebSocket frame boundaries [MQTT-6.0.0-2].
            await _webSocket.SendAsync(buffer, WebSocketMessageType.Binary, isEndOfPacket, cancellationToken).ConfigureAwait(false);
#else
            // The lock is required because the client will throw an exception if _SendAsync_ is 
            // called from multiple threads at the same time. But this issue only happens with several
            // framework versions.
            if (_sendLock == null)
            {
                return;
            }

            using (await _sendLock.EnterAsync(cancellationToken).ConfigureAwait(false))
            {
                await _webSocket.SendAsync(buffer, WebSocketMessageType.Binary, isEndOfPacket, cancellationToken).ConfigureAwait(false);
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
            throw new NotSupportedException("Proxies are not supported when using 'uap10.0'.");
#elif NETSTANDARD1_3
            throw new NotSupportedException("Proxies are not supported when using 'netstandard 1.3'.");
#else
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
#endif
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

#if !NETSTANDARD1_3
#if !WINDOWS_UWP
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
#endif
#endif
            if (_options.Credentials != null)
            {
                clientWebSocket.Options.Credentials = _options.Credentials;
            }
            
            var certificateValidationHandler = _options.TlsOptions?.CertificateValidationHandler;
            if (certificateValidationHandler != null)
            {
#if NETSTANDARD1_3
                throw new NotSupportedException("Remote certificate validation callback is not supported when using 'netstandard1.3'.");
#elif NETSTANDARD2_0
                throw new NotSupportedException("Remote certificate validation callback is not supported when using 'netstandard2.0'.");
#elif WINDOWS_UWP
                throw new NotSupportedException("Remote certificate validation callback is not supported when using 'uap10.0'.");
#elif NET452 || NET461 || NET48
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => {
                    var context = new MqttClientCertificateValidationEventArgs
                    {
                        Certificate = certificate,
                        Chain = chain,
                        SslPolicyErrors = sslPolicyErrors,
                        ClientOptions = _options
                    };

                    return certificateValidationHandler(context);
                };
#else
                clientWebSocket.Options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                {
                    // TODO: Find a way to add client options to same callback. Problem is that they have a different type.
                    var context = new MqttClientCertificateValidationEventArgs
                    {
                        Certificate = certificate,
                        Chain = chain,
                        SslPolicyErrors = sslPolicyErrors,
                        ClientOptions = _options
                    };

                    return certificateValidationHandler(context);
                };
#endif
            }
        }
    }
}
