#if NET452 || NET461 || NETSTANDARD1_3 || NETSTANDARD2_0
using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using MQTTnet.Channel;
using MQTTnet.Client;

namespace MQTTnet.Implementations
{
    public sealed class MqttTcpChannel : IMqttChannel, IDisposable
    {
        private readonly MqttClientTcpOptions _options;

        //todo: this can be used with min dependency NetStandard1.6
#if NET452 || NET461
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        public static int BufferSize { get; set; } = 4096 * 20; // Can be changed for fine tuning by library user.
#endif

        private Socket _socket;
        private SslStream _sslStream;

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
            _sslStream = sslStream;

            CreateStreams(socket, sslStream);
        }

        public Stream SendStream { get; private set; }
        public Stream ReceiveStream { get; private set; }

        public static Func<X509Certificate, X509Chain, SslPolicyErrors, MqttClientTcpOptions, bool> CustomCertificateValidationCallback { get; set; }

        public async Task ConnectAsync()
        {
            if (_socket == null)
            {
                _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            }

            //todo: else brach can be used with min dependency NET46
#if NET452 || NET461
            await Task.Factory.FromAsync(_socket.BeginConnect, _socket.EndConnect, _options.Server, _options.GetPort(), null).ConfigureAwait(false);
#else
            await _socket.ConnectAsync(_options.Server, _options.GetPort()).ConfigureAwait(false);
#endif

            if (_options.TlsOptions.UseTls)
            {
                _sslStream = new SslStream(new NetworkStream(_socket, true), false, InternalUserCertificateValidationCallback);
                await _sslStream.AuthenticateAsClientAsync(_options.Server, LoadCertificates(_options), SslProtocols.Tls12, _options.TlsOptions.IgnoreCertificateRevocationErrors).ConfigureAwait(false);
            }
            
            CreateStreams(_socket, _sslStream);
        }

        public Task DisconnectAsync()
        {
            Dispose();
            return Task.FromResult(0);
        }

        public void Dispose()
        {
            _socket?.Dispose();
            _socket = null;

            _sslStream?.Dispose();
            _sslStream = null;
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

        private static X509CertificateCollection LoadCertificates(MqttClientTcpOptions options)
        {
            var certificates = new X509CertificateCollection();
            if (options.TlsOptions.Certificates == null)
            {
                return certificates;
            }

            foreach (var certificate in options.TlsOptions.Certificates)
            {
                certificates.Add(new X509Certificate2(certificate));
            }

            return certificates;
        }

        private void CreateStreams(Socket socket, Stream sslStream)
        {
            var stream = sslStream ?? new NetworkStream(socket);
            
            //todo: if branch can be used with min dependency NetStandard1.6
#if NET452 || NET461
            SendStream = new BufferedStream(stream, BufferSize);
            ReceiveStream = new BufferedStream(stream, BufferSize);
#else
            SendStream = stream;
            ReceiveStream = stream;
#endif
        }

    }
}
#endif
