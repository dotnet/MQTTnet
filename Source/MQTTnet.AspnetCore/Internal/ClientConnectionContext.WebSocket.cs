// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http.Features;
using MQTTnet.Exceptions;
using System;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore
{
    partial class ClientConnectionContext
    {
        public static async Task<ClientConnectionContext> CreateAsync(MqttClientWebSocketOptions options, CancellationToken cancellationToken)
        {
            var uri = new Uri(options.Uri, UriKind.Absolute);
            if (uri.Scheme != Uri.UriSchemeWs && uri.Scheme != Uri.UriSchemeWss)
            {
                throw new MqttConfigurationException("The scheme of the WebSocket Uri must be ws or wss.");
            }

            // Patching TlsOptions
            options.TlsOptions ??= new MqttClientTlsOptions();
            // Scheme decides whether to use TLS
            options.TlsOptions.UseTls = uri.Scheme == Uri.UriSchemeWss;

            var clientWebSocket = new ClientWebSocket();
            try
            {
                SetupClientWebSocket(clientWebSocket.Options, options);
                await clientWebSocket.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Prevent a memory leak when always creating new instance which will fail while connecting.
                clientWebSocket.Dispose();
                throw;
            }

            var webSocketStream = new WebSocketStream(clientWebSocket);
            var connection = new ClientConnectionContext(webSocketStream)
            {
                LocalEndPoint = null,
                RemoteEndPoint = new DnsEndPoint(uri.Host, uri.Port),
            };

            connection.Features.Set(new WebSocketConnectionFeature(uri.AbsolutePath));
            if (uri.Scheme == Uri.UriSchemeWss)
            {
                connection.Features.Set<ITlsConnectionFeature>(TlsConnectionFeature.WithoutClientCertificate);
            }
            return connection;
        }

        private static void SetupClientWebSocket(ClientWebSocketOptions webSocketOptions, MqttClientWebSocketOptions options)
        {
            if (options.ProxyOptions != null)
            {
                webSocketOptions.Proxy = CreateProxy(options);
            }

            if (options.RequestHeaders != null)
            {
                foreach (var requestHeader in options.RequestHeaders)
                {
                    webSocketOptions.SetRequestHeader(requestHeader.Key, requestHeader.Value);
                }
            }

            if (options.SubProtocols != null)
            {
                foreach (var subProtocol in options.SubProtocols)
                {
                    webSocketOptions.AddSubProtocol(subProtocol);
                }
            }

            if (options.CookieContainer != null)
            {
                webSocketOptions.Cookies = options.CookieContainer;
            }

            if (options.TlsOptions.UseTls)
            {
                var certificates = options.TlsOptions.ClientCertificatesProvider?.GetCertificates();
                if (certificates?.Count > 0)
                {
                    webSocketOptions.ClientCertificates = certificates;
                }
            }

            // Only set the value if it is actually true. This property is not supported on all platforms
            // and will throw a _PlatformNotSupported_ (i.e. WASM) exception when being used regardless of the actual value.
            if (options.UseDefaultCredentials)
            {
                webSocketOptions.UseDefaultCredentials = options.UseDefaultCredentials;
            }

            if (options.KeepAliveInterval != WebSocket.DefaultKeepAliveInterval)
            {
                webSocketOptions.KeepAliveInterval = options.KeepAliveInterval;
            }

            if (options.Credentials != null)
            {
                webSocketOptions.Credentials = options.Credentials;
            }

            var certificateValidationHandler = options.TlsOptions.CertificateValidationHandler;
            if (certificateValidationHandler != null)
            {
                webSocketOptions.RemoteCertificateValidationCallback = (_, certificate, chain, sslPolicyErrors) =>
                {
                    // TODO: Find a way to add client options to same callback. Problem is that they have a different type.
                    var context = new MqttClientCertificateValidationEventArgs(certificate, chain, sslPolicyErrors, options);
                    return certificateValidationHandler(context);
                };

                var certificateSelectionHandler = options.TlsOptions.CertificateSelectionHandler;
                if (certificateSelectionHandler != null)
                {
                    throw new NotSupportedException("Remote certificate selection callback is not supported for WebSocket connections.");
                }
            }
        }

        private static IWebProxy? CreateProxy(MqttClientWebSocketOptions options)
        {
            if (!Uri.TryCreate(options.ProxyOptions?.Address, UriKind.Absolute, out var proxyUri))
            {
                return null;
            }


            WebProxy webProxy;
            if (!string.IsNullOrEmpty(options.ProxyOptions.Username) && !string.IsNullOrEmpty(options.ProxyOptions.Password))
            {
                var credentials = new NetworkCredential(options.ProxyOptions.Username, options.ProxyOptions.Password, options.ProxyOptions.Domain);
                webProxy = new WebProxy(proxyUri, options.ProxyOptions.BypassOnLocal, options.ProxyOptions.BypassList, credentials);
            }
            else
            {
                webProxy = new WebProxy(proxyUri, options.ProxyOptions.BypassOnLocal, options.ProxyOptions.BypassList);
            }

            if (options.ProxyOptions.UseDefaultCredentials)
            {
                // Only update the property if required because setting it to false will alter
                // the used credentials internally!
                webProxy.UseDefaultCredentials = true;
            }

            return webProxy;
        }


        private sealed class WebSocketStream(WebSocket webSocket) : Stream
        {
            private readonly WebSocket _webSocket = webSocket;

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => throw new NotSupportedException();
            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush() { }
            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                return _webSocket.SendAsync(buffer, WebSocketMessageType.Binary, false, cancellationToken);
            }

            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                var result = await _webSocket.ReceiveAsync(buffer, cancellationToken);
                return result.MessageType == WebSocketMessageType.Close ? 0 : result.Count;
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            protected override void Dispose(bool disposing)
            {
                _webSocket.Dispose();
                base.Dispose(disposing);
            }
        }
    }
}
