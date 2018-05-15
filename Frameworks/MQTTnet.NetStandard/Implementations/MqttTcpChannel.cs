#if NET452 || NET461 || NETSTANDARD1_3 || NETSTANDARD2_0
using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Threading;
using MQTTnet.Channel;
using MQTTnet.Client;

namespace MQTTnet.Implementations
{
    public sealed class MqttTcpChannel : IMqttChannel
    {
        private readonly MqttClientTcpOptions _options;

        private Socket _socket;
        private Stream _stream;

        /// <summary>
        /// called on client sockets are created in connect
        /// </summary>
        public MqttTcpChannel(MqttClientTcpOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// called on server, sockets are passed in
        /// connect will not be called
        /// </summary>
        public MqttTcpChannel(Socket socket, SslStream sslStream)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));

            CreateStream(sslStream);
        }

        public static Func<X509Certificate, X509Chain, SslPolicyErrors, MqttClientTcpOptions, bool> CustomCertificateValidationCallback { get; set; }

        public string Endpoint => _socket?.RemoteEndPoint?.ToString();

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            if (_socket == null)
            {
                _socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            }

#if NET452 || NET461
            await Task.Factory.FromAsync(_socket.BeginConnect, _socket.EndConnect, _options.Server, _options.GetPort(), null).ConfigureAwait(false);
#else
            await _socket.ConnectAsync(_options.Server, _options.GetPort()).ConfigureAwait(false);
#endif

            SslStream sslStream = null;
            if (_options.TlsOptions.UseTls)
            {
                sslStream = new SslStream(new NetworkStream(_socket, true), false, InternalUserCertificateValidationCallback);
                await sslStream.AuthenticateAsClientAsync(_options.Server, LoadCertificates(), SslProtocols.Tls12, _options.TlsOptions.IgnoreCertificateRevocationErrors).ConfigureAwait(false);
            }

            CreateStream(sslStream);
        }

        public Task DisconnectAsync()
        {
            Dispose();
            return Task.FromResult(0);
        }

        public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _stream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _stream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public void Dispose()
        {
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
            finally
            {
                _stream = null;
            }

            try
            {
                _socket?.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (NullReferenceException)
            {
            }
            finally
            {
                _socket = null;
            }
        }

        private bool InternalUserCertificateValidationCallback(object sender, X509Certificate x509Certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (CustomCertificateValidationCallback != null)
            {
                return CustomCertificateValidationCallback(x509Certificate, chain, sslPolicyErrors, _options);
            }

            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            if (chain.ChainStatus.Any(c => c.Status == X509ChainStatusFlags.RevocationStatusUnknown || c.Status == X509ChainStatusFlags.Revoked || c.Status == X509ChainStatusFlags.RevocationStatusUnknown))
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
