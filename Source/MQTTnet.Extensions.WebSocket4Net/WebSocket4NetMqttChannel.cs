// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Channel;
using MQTTnet.Client;
using MQTTnet.Exceptions;
using SuperSocket.ClientEngine;
using WebSocket4Net;

namespace MQTTnet.Extensions.WebSocket4Net
{
    public sealed class WebSocket4NetMqttChannel : IMqttChannel
    {
        readonly BlockingCollection<byte> _receiveBuffer = new BlockingCollection<byte>();

        readonly MqttClientOptions _clientOptions;
        readonly MqttClientWebSocketOptions _webSocketOptions;

        WebSocket _webSocket;

        public WebSocket4NetMqttChannel(MqttClientOptions clientOptions, MqttClientWebSocketOptions webSocketOptions)
        {
            _clientOptions = clientOptions ?? throw new ArgumentNullException(nameof(clientOptions));
            _webSocketOptions = webSocketOptions ?? throw new ArgumentNullException(nameof(webSocketOptions));
        }

        public string Endpoint => _webSocketOptions.Uri;

        public bool IsSecureConnection { get; private set; }

        public X509Certificate2 ClientCertificate { get; }

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            var uri = _webSocketOptions.Uri;
            if (!uri.StartsWith("ws://", StringComparison.OrdinalIgnoreCase) && !uri.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
            {
                if (_webSocketOptions.TlsOptions?.UseTls == false)
                {
                    uri = "ws://" + uri;
                }
                else
                {
                    uri = "wss://" + uri;
                }
            }

#if NET48 || NETCOREAPP3_1 || NET5 || NET6
            var sslProtocols = _webSocketOptions?.TlsOptions?.SslProtocol ?? SslProtocols.Tls12 | SslProtocols.Tls13;
#else
            var sslProtocols = _webSocketOptions?.TlsOptions?.SslProtocol ?? SslProtocols.Tls12 | (SslProtocols)0x00003000 /*Tls13*/;
#endif

            var subProtocol = _webSocketOptions.SubProtocols.FirstOrDefault() ?? string.Empty;

            var cookies = new List<KeyValuePair<string, string>>();
            if (_webSocketOptions.CookieContainer != null)
            {
                throw new NotSupportedException("Cookies are not supported.");
            }

            List<KeyValuePair<string, string>> customHeaders = null;
            if (_webSocketOptions.RequestHeaders != null)
            {
                customHeaders = _webSocketOptions.RequestHeaders.Select(i => new KeyValuePair<string, string>(i.Key, i.Value)).ToList();
            }

            EndPoint proxy = null;
            if (_webSocketOptions.ProxyOptions != null)
            {
                throw new NotSupportedException("Proxies are not supported.");
            }

            // The user agent can be empty always because it is just added to the custom headers as "User-Agent".
            var userAgent = string.Empty;

            var origin = string.Empty;
            var webSocketVersion = WebSocketVersion.None;
            var receiveBufferSize = 0;

            var certificates = new X509CertificateCollection();
            if (_webSocketOptions?.TlsOptions?.Certificates != null)
            {
                foreach (var certificate in _webSocketOptions.TlsOptions.Certificates)
                {
#if WINDOWS_UWP
                    certificates.Add(new X509Certificate(certificate));
#else
                    certificates.Add(certificate);
#endif
                }
            }

            _webSocket = new WebSocket(uri, subProtocol, cookies, customHeaders, userAgent, origin, webSocketVersion, proxy, sslProtocols, receiveBufferSize)
            {
                NoDelay = true,
                Security =
                {
                    AllowUnstrustedCertificate = _webSocketOptions?.TlsOptions?.AllowUntrustedCertificates == true,
                    AllowCertificateChainErrors = _webSocketOptions?.TlsOptions?.IgnoreCertificateChainErrors == true,
                    Certificates = certificates
                }
            };

            await ConnectInternalAsync(cancellationToken).ConfigureAwait(false);
            
            IsSecureConnection = uri.StartsWith("wss://", StringComparison.OrdinalIgnoreCase);
        }

        public Task DisconnectAsync(CancellationToken cancellationToken)
        {
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                _webSocket.Close();
            }
            
            return Task.FromResult(0);
        }

        public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var readBytes = 0;
            while (count > 0 && !cancellationToken.IsCancellationRequested)
            {
                if (!_receiveBuffer.TryTake(out var @byte))
                {
                    if (readBytes == 0)
                    {
                        // Block until at least one byte was received.
                        @byte = _receiveBuffer.Take(cancellationToken);
                    }
                    else
                    {
                        return Task.FromResult(readBytes);
                    }
                }

                buffer[offset] = @byte;
                offset++;
                count--;
                readBytes++;
            }

            return Task.FromResult(readBytes);
        }

        public Task WriteAsync(ArraySegment<byte> buffer, bool isEndOfPacket, CancellationToken cancellationToken)
        {
            _webSocket.Send(buffer.Array, buffer.Offset, buffer.Count);
            return Task.FromResult(0);
        }

        public void Dispose()
        {
            if (_webSocket == null)
            {
                return;
            }

            _webSocket.DataReceived -= OnDataReceived;
            _webSocket.Error -= OnError;
            _webSocket.Dispose();
            _webSocket = null;
        }

        static void OnError(object sender, ErrorEventArgs e)
        {
            System.Diagnostics.Debug.Write(e.Exception.ToString());
        }

        void OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            foreach (var @byte in e.Data)
            {
                _receiveBuffer.Add(@byte);
            }
        }

        async Task ConnectInternalAsync(CancellationToken cancellationToken)
        {
            _webSocket.Error += OnError;
            _webSocket.DataReceived += OnDataReceived;

            var taskCompletionSource = new TaskCompletionSource<Exception>();

            void ErrorHandler(object sender, ErrorEventArgs e)
            {
                taskCompletionSource.TrySetResult(e.Exception);
            }

            void SuccessHandler(object sender, EventArgs e)
            {
                taskCompletionSource.TrySetResult(null);
            }

            try
            {
                _webSocket.Opened += SuccessHandler;
                _webSocket.Error += ErrorHandler;

#pragma warning disable AsyncFixer02 // Long-running or blocking operations inside an async method
                _webSocket.Open();
#pragma warning restore AsyncFixer02 // Long-running or blocking operations inside an async method

                using (var timeoutCts = new CancellationTokenSource(_clientOptions.Timeout))
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken))
                {
                    using (linkedCts.Token.Register(() => taskCompletionSource.TrySetCanceled()))
                    {
                        try
                        {
                            await taskCompletionSource.Task.ConfigureAwait(false);
                        }
                        catch (Exception exception)
                        {
                            var timeoutReached = timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested;
                            if (timeoutReached)
                            {
                                throw new MqttCommunicationTimedOutException(exception);
                            }
                            
                            if (exception is AuthenticationException authenticationException)
                            {
                                throw new MqttCommunicationException(authenticationException.InnerException);
                            }

                            if (exception is OperationCanceledException)
                            {
                                throw new MqttCommunicationTimedOutException();
                            }

                            throw new MqttCommunicationException(exception);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw new MqttCommunicationTimedOutException();
            }
            finally
            {
                _webSocket.Opened -= SuccessHandler;
                _webSocket.Error -= ErrorHandler;
            }
        }
    }
}
