using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using MQTTnet.Core.Channel;
using MQTTnet.Core.Client;
using MQTTnet.Core.Exceptions;
using System.IO;

namespace MQTTnet.Implementations
{
    public sealed class MqttTcpChannel : IMqttCommunicationChannel, IDisposable
    {
        private Socket _socket;
        private SslStream _sslStream;
        
        public Stream ReceiveStream { get; private set; }
        public Stream RawStream => ReceiveStream;
        public Stream SendStream => ReceiveStream;

        /// <summary>
        /// called on client sockets are created in connect
        /// </summary>
        public MqttTcpChannel()
        {
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

        public async Task ConnectAsync(MqttClientOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            try
            {
                if (_socket == null)
                {
                    _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                }

                await _socket.ConnectAsync(options.Server, options.GetPort()).ConfigureAwait(false);

                if (options.TlsOptions.UseTls)
                {
                    _sslStream = new SslStream(new NetworkStream(_socket, true));
                    ReceiveStream = _sslStream;
                    await _sslStream.AuthenticateAsClientAsync(options.Server, LoadCertificates(options), SslProtocols.Tls12, options.TlsOptions.CheckCertificateRevocation).ConfigureAwait(false);
                }
                else
                {
                    ReceiveStream = new NetworkStream(_socket);
                }
            }
            catch (SocketException exception)
            {
                throw new MqttCommunicationException(exception);
            }
        }

        public Task DisconnectAsync()
        {
            try
            {
                Dispose();
                return Task.FromResult(0);
            }
            catch (SocketException exception)
            {
                throw new MqttCommunicationException(exception);
            }
        }

        public void Dispose()
        {
            _socket?.Dispose();
            _sslStream?.Dispose();

            _socket = null;
            _sslStream = null;
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