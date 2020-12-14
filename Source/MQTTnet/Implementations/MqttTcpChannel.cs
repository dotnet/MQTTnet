#if !WINDOWS_UWP
using MQTTnet.Channel;
using MQTTnet.Client.Options;
using System;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Exceptions;

namespace MQTTnet.Implementations
{
    public sealed class MqttTcpChannel : IMqttChannel
    {
        readonly IMqttClientOptions _clientOptions;
        readonly MqttClientTcpOptions _tcpOptions;

        Stream _stream;

        public MqttTcpChannel(IMqttClientOptions clientOptions)
        {
            _clientOptions = clientOptions ?? throw new ArgumentNullException(nameof(clientOptions));
            _tcpOptions = (MqttClientTcpOptions)clientOptions.ChannelOptions;

            IsSecureConnection = clientOptions.ChannelOptions?.TlsOptions?.UseTls == true;
        }

        public MqttTcpChannel(Stream stream, string endpoint, X509Certificate2 clientCertificate)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));

            Endpoint = endpoint;

            IsSecureConnection = stream is SslStream;
            ClientCertificate = clientCertificate;
        }

        public string Endpoint { get; private set; }

        public bool IsSecureConnection { get; }

        public X509Certificate2 ClientCertificate { get; }

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            CrossPlatformSocket socket = null;
            try
            {
                if (_tcpOptions.AddressFamily == AddressFamily.Unspecified)
                {
                    socket = new CrossPlatformSocket();
                }
                else
                {
                    socket = new CrossPlatformSocket(_tcpOptions.AddressFamily);
                }

                socket.ReceiveBufferSize = _tcpOptions.BufferSize;
                socket.SendBufferSize = _tcpOptions.BufferSize;
                socket.SendTimeout = (int)_clientOptions.CommunicationTimeout.TotalMilliseconds;
                socket.NoDelay = _tcpOptions.NoDelay;

                if (_tcpOptions.DualMode.HasValue)
                {
                    // It is important to avoid setting the flag if no specific value is set by the user
                    // because on IPv4 only networks the setter will always throw an exception. Regardless
                    // of the actual value.
                    socket.DualMode = _tcpOptions.DualMode.Value;
                }

                await socket.ConnectAsync(_tcpOptions.Server, _tcpOptions.GetPort(), cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                var networkStream = socket.GetStream();
                
                if (_tcpOptions.TlsOptions?.UseTls == true)
                {
                    var sslStream = new SslStream(networkStream, false, InternalUserCertificateValidationCallback);
                    try
                    {
#if NETCOREAPP3_1
                        var sslOptions = new SslClientAuthenticationOptions
                        {
                            ApplicationProtocols = _tcpOptions.TlsOptions.ApplicationProtocols,
                            ClientCertificates = LoadCertificates(),
                            EnabledSslProtocols = _tcpOptions.TlsOptions.SslProtocol,
                            CertificateRevocationCheckMode = _tcpOptions.TlsOptions.IgnoreCertificateRevocationErrors ? X509RevocationMode.NoCheck : X509RevocationMode.Online,
                            TargetHost = _tcpOptions.Server
                        };

                        await sslStream.AuthenticateAsClientAsync(sslOptions, cancellationToken).ConfigureAwait(false);
#else 
                        await sslStream.AuthenticateAsClientAsync(_tcpOptions.Server, LoadCertificates(), _tcpOptions.TlsOptions.SslProtocol, !_tcpOptions.TlsOptions.IgnoreCertificateRevocationErrors).ConfigureAwait(false);
#endif
                    }
                    catch
                    {
#if NETSTANDARD2_1 || NETCOREAPP3_1
                        await sslStream.DisposeAsync().ConfigureAwait(false);
#else
                        sslStream.Dispose();
#endif

                        throw;
                    }

                    _stream = sslStream;
                }
                else
                {
                    _stream = networkStream;
                }

                Endpoint = socket.RemoteEndPoint?.ToString();
            }
            catch (Exception)
            {
                socket?.Dispose();
                throw;
            }
        }

        public Task DisconnectAsync(CancellationToken cancellationToken)
        {
            Dispose();
            return Task.FromResult(0);
        }

        public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var stream = _stream;

                if (stream == null)
                {
                    return 0;
                }

                if (!stream.CanRead)
                {
                    return 0;
                }

                // Workaround for: https://github.com/dotnet/corefx/issues/24430
                using (cancellationToken.Register(Dispose))
                {
                    return await stream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (ObjectDisposedException)
            {
                // Indicate a graceful socket close.
                return 0;
            }
            catch (IOException exception)
            {
                if (exception.InnerException is SocketException socketException)
                {
                    ExceptionDispatchInfo.Capture(socketException).Throw();
                }

                throw;
            }
        }

        public async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Workaround for: https://github.com/dotnet/corefx/issues/24430
                using (cancellationToken.Register(Dispose))
                {
                    var stream = _stream;

                    if (stream == null)
                    {
                        throw new MqttCommunicationException("The TCP connection is closed.");
                    }

                    await stream.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (ObjectDisposedException)
            {
                throw new MqttCommunicationException("The TCP connection is closed.");
            }
            catch (IOException exception)
            {
                if (exception.InnerException is SocketException socketException)
                {
                    ExceptionDispatchInfo.Capture(socketException).Throw();
                }

                throw;
            }
        }

        public void Dispose()
        {
            // When the stream is disposed it will also close the socket and this will also dispose it.
            // So there is no need to dispose the socket again.
            // https://stackoverflow.com/questions/3601521/should-i-manually-dispose-the-socket-after-closing-it
            try
            {
                _stream?.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (NullReferenceException)
            {
            }

            _stream = null;
        }

        bool InternalUserCertificateValidationCallback(object sender, X509Certificate x509Certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            #region OBSOLETE

#pragma warning disable CS0618 // Type or member is obsolete
            var certificateValidationCallback = _tcpOptions?.TlsOptions?.CertificateValidationCallback;
#pragma warning restore CS0618 // Type or member is obsolete
            if (certificateValidationCallback != null)
            {
                return certificateValidationCallback(x509Certificate, chain, sslPolicyErrors, _clientOptions);
            }
            #endregion

            var certificateValidationHandler = _tcpOptions?.TlsOptions?.CertificateValidationHandler;
            if (certificateValidationHandler != null)
            {
                var context = new MqttClientCertificateValidationCallbackContext
                {
                    Certificate = x509Certificate,
                    Chain = chain,
                    SslPolicyErrors = sslPolicyErrors,
                    ClientOptions = _tcpOptions
                };

                return certificateValidationHandler(context);
            }

            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            if (chain.ChainStatus.Any(c => c.Status == X509ChainStatusFlags.RevocationStatusUnknown || c.Status == X509ChainStatusFlags.Revoked || c.Status == X509ChainStatusFlags.OfflineRevocation))
            {
                if (_tcpOptions?.TlsOptions?.IgnoreCertificateRevocationErrors != true)
                {
                    return false;
                }
            }

            if (chain.ChainStatus.Any(c => c.Status == X509ChainStatusFlags.PartialChain))
            {
                if (_tcpOptions?.TlsOptions?.IgnoreCertificateChainErrors != true)
                {
                    return false;
                }
            }

            return _tcpOptions?.TlsOptions?.AllowUntrustedCertificates == true;
        }

        X509CertificateCollection LoadCertificates()
        {
            var certificates = new X509CertificateCollection();
            if (_tcpOptions.TlsOptions.Certificates == null)
            {
                return certificates;
            }

            foreach (var certificate in _tcpOptions.TlsOptions.Certificates)
            {
                certificates.Add(certificate);
            }

            return certificates;
        }
    }
}
#endif
