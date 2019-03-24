#if !WINDOWS_UWP
using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Threading;
using MQTTnet.Channel;
using MQTTnet.Client.Options;

namespace MQTTnet.Implementations
{
    public class MqttTcpChannel : IMqttChannel
    {
        private readonly IMqttClientOptions _clientOptions;
        private readonly MqttClientTcpOptions _options;

        private Socket _socket;
        private Stream _stream;

        /// <summary>
        /// called on client sockets are created in connect
        /// </summary>
        public MqttTcpChannel(IMqttClientOptions clientOptions)
        {
            _clientOptions = clientOptions ?? throw new ArgumentNullException(nameof(clientOptions));
            _options = (MqttClientTcpOptions)clientOptions.ChannelOptions;

            IsSecureConnection = clientOptions.ChannelOptions?.TlsOptions?.UseTls == true;
        }

        /// <summary>
        /// called on server, sockets are passed in
        /// connect will not be called
        /// </summary>
        public MqttTcpChannel(Socket socket, SslStream sslStream)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));

            IsSecureConnection = sslStream != null;

            CreateStream(sslStream);
        }

        public string Endpoint => _socket?.RemoteEndPoint?.ToString();

        public bool IsSecureConnection { get; }

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            if (_socket == null)
            {
                _socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            }

            // Workaround for: workaround for https://github.com/dotnet/corefx/issues/24430
            using (cancellationToken.Register(() => _socket.Dispose()))
            {
#if NET452 || NET461
                await Task.Factory.FromAsync(_socket.BeginConnect, _socket.EndConnect, _options.Server, _options.GetPort(), null).ConfigureAwait(false);
#else
                await _socket.ConnectAsync(_options.Server, _options.GetPort()).ConfigureAwait(false);
#endif
            }

            SslStream sslStream = null;
            if (_options.TlsOptions.UseTls)
            {
                sslStream = new SslStream(new NetworkStream(_socket, true), false, InternalUserCertificateValidationCallback);
                await sslStream.AuthenticateAsClientAsync(_options.Server, LoadCertificates(), _options.TlsOptions.SslProtocol, _options.TlsOptions.IgnoreCertificateRevocationErrors).ConfigureAwait(false);
            }

            CreateStream(sslStream);
        }

        public Task DisconnectAsync(CancellationToken cancellationToken)
        {
            Dispose();
            return Task.FromResult(0);
        }

        public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
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

        public async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // Workaround for: https://github.com/dotnet/corefx/issues/24430
            using (cancellationToken.Register(Dispose))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await _stream.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
                //await _stream.FlushAsync(cancellationToken);
            }
        }

        public void Dispose()
        {
            _socket = null;

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

        private void CreateStream(Stream stream)
        {
            if (stream != null)
            {
                _stream = stream;
            }
            else
            {
                _stream = new NetworkStream(_socket, true);
            }
        }
    }
}
#endif
