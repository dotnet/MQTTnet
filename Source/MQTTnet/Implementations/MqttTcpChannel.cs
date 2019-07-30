#if !WINDOWS_UWP
using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using MQTTnet.Channel;
using MQTTnet.Client.Options;

namespace MQTTnet.Implementations
{
    public class MqttTcpChannel : IMqttChannel
    {
        private readonly IMqttClientOptions _clientOptions;
        private readonly MqttClientTcpOptions _options;

        private Stream _stream;

        public MqttTcpChannel(IMqttClientOptions clientOptions)
        {
            _clientOptions = clientOptions ?? throw new ArgumentNullException(nameof(clientOptions));
            _options = (MqttClientTcpOptions)clientOptions.ChannelOptions;

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
            Socket socket;

            if (_options.AddressFamily == AddressFamily.Unspecified)
            {
                socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            }
            else
            {
                socket = new Socket(_options.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            }

            socket.ReceiveBufferSize = _options.BufferSize;
            socket.SendBufferSize = _options.BufferSize;
            socket.NoDelay = _options.NoDelay;

            if (_options.DualMode.HasValue)
            {
                // It is important to avoid setting the flag if no specific value is set by the user
                // because on IPv4 only networks the setter will always throw an exception. Regardless
                // of the actual value.
                socket.DualMode = _options.DualMode.Value;
            }
            
            // Workaround for: workaround for https://github.com/dotnet/corefx/issues/24430
            using (cancellationToken.Register(() => socket.Dispose()))
            {
#if NET452 || NET461
                await Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, _options.Server, _options.GetPort(), null).ConfigureAwait(false);
#else
                await socket.ConnectAsync(_options.Server, _options.GetPort()).ConfigureAwait(false);
#endif
            }

            var networkStream = new NetworkStream(socket, true);

            if (_options.TlsOptions.UseTls)
            {
                var sslStream = new SslStream(networkStream, false, InternalUserCertificateValidationCallback);
                _stream = sslStream;

                await sslStream.AuthenticateAsClientAsync(_options.Server, LoadCertificates(), _options.TlsOptions.SslProtocol, _options.TlsOptions.IgnoreCertificateRevocationErrors).ConfigureAwait(false);                
            }
            else
            {
                _stream = networkStream;
            }

            Endpoint = socket.RemoteEndPoint?.ToString();
        }

        public Task DisconnectAsync(CancellationToken cancellationToken)
        {
            Dispose();
            return Task.FromResult(0);
        }

        public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            try
            {
                // Workaround for: https://github.com/dotnet/corefx/issues/24430
                using (cancellationToken.Register(Dispose))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return 0;
                    }

                    return await _stream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
                }
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
            try
            {
                // Workaround for: https://github.com/dotnet/corefx/issues/24430
                using (cancellationToken.Register(Dispose))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    await _stream.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
                }
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

        private bool InternalUserCertificateValidationCallback(object sender, X509Certificate x509Certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (_options.TlsOptions.CertificateValidationCallback != null)
            {
                return _options.TlsOptions.CertificateValidationCallback(x509Certificate, chain, sslPolicyErrors, _clientOptions);
            }

            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            if (chain.ChainStatus.Any(c => c.Status == X509ChainStatusFlags.RevocationStatusUnknown || c.Status == X509ChainStatusFlags.Revoked || c.Status == X509ChainStatusFlags.OfflineRevocation))
            {
                if (!_options.TlsOptions.IgnoreCertificateRevocationErrors)
                {
                    return false;
                }
            }

            if (chain.ChainStatus.Any(c => c.Status == X509ChainStatusFlags.PartialChain))
            {
                if (!_options.TlsOptions.IgnoreCertificateChainErrors)
                {
                    return false;
                }
            }

            return _options.TlsOptions.AllowUntrustedCertificates;
        }

        private X509CertificateCollection LoadCertificates()
        {
            var certificates = new X509CertificateCollection();
            if (_options.TlsOptions.Certificates == null)
            {
                return certificates;
            }

            foreach (var certificate in _options.TlsOptions.Certificates)
            {
                certificates.Add(new X509Certificate2(certificate));
            }

            return certificates;
        }
    }
}
#endif
