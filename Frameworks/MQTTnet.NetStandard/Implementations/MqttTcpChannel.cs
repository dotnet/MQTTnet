using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using MQTTnet.Core.Channel;
using MQTTnet.Core.Client;
using System.IO;

namespace MQTTnet.Implementations
{
    public sealed class MqttTcpChannel : IMqttCommunicationChannel, IDisposable
    {
        private readonly MqttClientTcpOptions _options;

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
            ReceiveStream = (Stream)sslStream ?? new NetworkStream(socket);
        }

        public Stream SendStream => ReceiveStream;
        public Stream ReceiveStream { get; private set; }
        public Stream RawReceiveStream => ReceiveStream;
        
        public async Task ConnectAsync()
        {
            if (_socket == null)
            {
                _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            }

            await _socket.ConnectAsync(_options.Server, _options.GetPort()).ConfigureAwait(false);

            if (_options.TlsOptions.UseTls)
            {
                _sslStream = new SslStream(new NetworkStream(_socket, true), false, UserCertificateValidationCallback);
                ReceiveStream = _sslStream;
                await _sslStream.AuthenticateAsClientAsync(_options.Server, LoadCertificates(_options), SslProtocols.Tls12, _options.TlsOptions.CheckCertificateRevocation).ConfigureAwait(false);
            }
            else
            {
                ReceiveStream = new NetworkStream(_socket);
            }
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

        private bool UserCertificateValidationCallback(object sender, X509Certificate x509Certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0)
            {
                return _options.TlsOptions.IgnoreCertificateChainErrors;
            }

            return false;
        }

        private static X509CertificateCollection LoadCertificates(MqttClientOptions options)
        {
            var certificates = new X509CertificateCollection();
            if (options.TlsOptions.Certificates == null)
            {
                return certificates;
            }

            foreach (var certificate in options.TlsOptions.Certificates)
            {
                certificates.Add(new X509Certificate(certificate));
            }

            return certificates;
        }
    }
}