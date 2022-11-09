// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !WINDOWS_UWP
using MQTTnet.Channel;
using MQTTnet.Client;
using MQTTnet.Exceptions;
using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Internal;

namespace MQTTnet.Implementations
{
    public sealed class MqttTcpChannel : IMqttChannel
    {
        readonly MqttClientOptions _clientOptions;
        readonly Action _disposeAction;
        readonly MqttClientTcpOptions _tcpOptions;

        Stream _stream;

        public MqttTcpChannel()
        {
            _disposeAction = Dispose;
        }

        public MqttTcpChannel(MqttClientOptions clientOptions) : this()
        {
            _clientOptions = clientOptions ?? throw new ArgumentNullException(nameof(clientOptions));
            _tcpOptions = (MqttClientTcpOptions)clientOptions.ChannelOptions;

            IsSecureConnection = clientOptions.ChannelOptions?.TlsOptions?.UseTls == true;
        }

        public MqttTcpChannel(Stream stream, string endpoint, X509Certificate2 clientCertificate) : this()
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));

            Endpoint = endpoint;

            IsSecureConnection = stream is SslStream;
            ClientCertificate = clientCertificate;
        }

        public X509Certificate2 ClientCertificate { get; }

        public string Endpoint { get; private set; }

        public bool IsSecureConnection { get; }

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

                if (_tcpOptions.LocalEndpoint != null)
                {
                    socket.Bind(_tcpOptions.LocalEndpoint);
                }
                
                socket.ReceiveBufferSize = _tcpOptions.BufferSize;
                socket.SendBufferSize = _tcpOptions.BufferSize;
                socket.SendTimeout = (int)_clientOptions.Timeout.TotalMilliseconds;
                socket.NoDelay = _tcpOptions.NoDelay;

                if (socket.LingerState != null)
                {
                    socket.LingerState = _tcpOptions.LingerState;
                }

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
#if NETCOREAPP3_1 || NET5_0_OR_GREATER
                        var sslOptions = new SslClientAuthenticationOptions
                        {
                            ApplicationProtocols = _tcpOptions.TlsOptions.ApplicationProtocols,
                            ClientCertificates = LoadCertificates(),
                            EnabledSslProtocols = _tcpOptions.TlsOptions.SslProtocol,
                            CertificateRevocationCheckMode =
 _tcpOptions.TlsOptions.IgnoreCertificateRevocationErrors ? X509RevocationMode.NoCheck : _tcpOptions.TlsOptions.RevocationMode,
                            TargetHost = _tcpOptions.Server,
                            CipherSuitesPolicy = _tcpOptions.TlsOptions.CipherSuitesPolicy
                        };

                        await sslStream.AuthenticateAsClientAsync(sslOptions, cancellationToken).ConfigureAwait(false);
#else
                        await sslStream.AuthenticateAsClientAsync(
                                _tcpOptions.Server,
                                LoadCertificates(),
                                _tcpOptions.TlsOptions.SslProtocol,
                                !_tcpOptions.TlsOptions.IgnoreCertificateRevocationErrors)
                            .ConfigureAwait(false);
#endif
                    }
                    catch
                    {
#if NETSTANDARD2_1 || NETCOREAPP3_1 || NET5_0_OR_GREATER
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
            return CompletedTask.Instance;
        }

        public void Dispose()
        {
            // When the stream is disposed it will also close the socket and this will also dispose it.
            // So there is no need to dispose the socket again.
            // https://stackoverflow.com/questions/3601521/should-i-manually-dispose-the-socket-after-closing-it
            try
            {
#if !NETSTANDARD1_3
                _stream?.Close();
#endif
                _stream?.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (NullReferenceException)
            {
            }
            finally
            {
                _stream = null;
            }
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

#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                return await stream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
#else
                // Workaround for: https://github.com/dotnet/corefx/issues/24430
                using (cancellationToken.Register(_disposeAction))
                {
                    return await stream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
                }
#endif
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

        public async Task WriteAsync(ArraySegment<byte> buffer, bool isEndOfPacket, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var stream = _stream;

                if (stream == null)
                {
                    throw new MqttCommunicationException("The TCP connection is closed.");
                }

#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                await stream.WriteAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
#else
                // Workaround for: https://github.com/dotnet/corefx/issues/24430
                using (cancellationToken.Register(_disposeAction))
                {
                    await stream.WriteAsync(buffer.Array, buffer.Offset, buffer.Count, cancellationToken).ConfigureAwait(false);
                }
#endif
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

        bool InternalUserCertificateValidationCallback(object sender, X509Certificate x509Certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            var certificateValidationHandler = _tcpOptions?.TlsOptions?.CertificateValidationHandler;
            if (certificateValidationHandler != null)
            {
                var eventArgs = new MqttClientCertificateValidationEventArgs
                {
                    Certificate = x509Certificate,
                    Chain = chain,
                    SslPolicyErrors = sslPolicyErrors,
                    ClientOptions = _tcpOptions
                };

                return certificateValidationHandler(eventArgs);
            }

            if (_tcpOptions?.TlsOptions?.IgnoreCertificateChainErrors ?? false)
            {
                sslPolicyErrors &= ~SslPolicyErrors.RemoteCertificateChainErrors;
            }

            return sslPolicyErrors == SslPolicyErrors.None;
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