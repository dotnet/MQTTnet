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
        private Stream _dataStream;
        private Socket _socket;
        private SslStream _sslStream;

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
            _dataStream = (Stream)sslStream ?? new NetworkStream(socket);
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

                await Task.Factory.FromAsync(_socket.BeginConnect, _socket.EndConnect, options.Server, options.GetPort(), null).ConfigureAwait(false);

                if (options.TlsOptions.UseTls)
                {
                    _sslStream = new SslStream(new NetworkStream(_socket, true));
                    
                    _dataStream = _sslStream;
                    await _sslStream.AuthenticateAsClientAsync(options.Server, LoadCertificates(options), SslProtocols.Tls12, options.TlsOptions.CheckCertificateRevocation).ConfigureAwait(false);
                }
                else
                {
                    _dataStream = new NetworkStream(_socket);
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

        public async Task WriteAsync(byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            try
            {
                await _dataStream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (SocketException exception)
            {
                throw new MqttCommunicationException(exception);
            }
        }

        public async Task<ArraySegment<byte>> ReadAsync(int length, byte[] buffer)
        {
            try
            {
                var totalBytes = 0;

                do
                {
                    var read = await _dataStream.ReadAsync(buffer, totalBytes, length - totalBytes).ConfigureAwait(false);
                    if (read == 0)
                    {
                        throw new MqttCommunicationException(new SocketException((int)SocketError.Disconnecting));
                    }

                    totalBytes += read;
                }
                while (totalBytes < length);
                return new ArraySegment<byte>(buffer, 0, length);
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

        public int Peek()
        {
            return _socket.Available;
        }
    }
}