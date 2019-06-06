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
using MQTTnet.Client.Options;
using MQTTnet.Exceptions;
using MQTTnet.Internal;
using SuperSocket.ClientEngine;
using WebSocket4Net;

namespace MQTTnet.Extensions.WebSocket4Net
{
    public class WebSocket4NetMqttChannel : IMqttChannel
    {
        private readonly BlockingCollection<byte> _receiveBuffer = new BlockingCollection<byte>();

        private readonly IMqttClientOptions _clientOptions;
        private readonly MqttClientWebSocketOptions _webSocketOptions;

        private WebSocket _webSocket;

        public WebSocket4NetMqttChannel(IMqttClientOptions clientOptions, MqttClientWebSocketOptions webSocketOptions)
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

            var sslProtocols = _webSocketOptions?.TlsOptions?.SslProtocol ?? SslProtocols.None;
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
                    certificates.Add(new X509Certificate(certificate));
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

        public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            _webSocket.Send(buffer, offset, count);
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

        private void OnError(object sender, ErrorEventArgs e)
        {
            System.Diagnostics.Debug.Write(e.Exception.ToString());
        }

        private void OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            foreach (var @byte in e.Data)
            {
                _receiveBuffer.Add(@byte);
            }
        }

        private async Task ConnectInternalAsync(CancellationToken cancellationToken)
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

                _webSocket.Open();

                var exception = await MqttTaskTimeout.WaitAsync(c =>
                {
                    c.Register(() => taskCompletionSource.TrySetCanceled());
                    return taskCompletionSource.Task;
                }, _clientOptions.CommunicationTimeout, cancellationToken).ConfigureAwait(false);

                if (exception != null)
                {
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
