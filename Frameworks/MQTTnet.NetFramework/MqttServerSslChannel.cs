using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using MQTTnet.Core.Channel;
using MQTTnet.Core.Client;
using MQTTnet.Core.Exceptions;

namespace MQTTnet
{
    /// <summary>
    /// Describes an SSL channel to a client.
    /// </summary>
    public class MqttServerSslChannel : IMqttCommunicationChannel, IDisposable
    {
        private readonly Socket _socket;
        private SslStream _sslStream;
        private X509Certificate2 _cert;
        
        /// <summary>
        /// Creates a new <see cref="MqttClientSslChannel"/> with a predefined <paramref name="socket"/>.
        /// </summary>
        /// <param name="socket">The client socket.</param>
        /// <param name="cert">The X509 certificate used to authenticate as a server.</param>
        public MqttServerSslChannel(Socket socket, X509Certificate2 cert)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            _cert = cert ?? throw new ArgumentNullException(nameof(cert));

            if (!_socket.Connected)
                return;

            NetworkStream ns = new NetworkStream(_socket, true);
            _sslStream = new SslStream(ns);
        }

        public async Task Authenticate()
        {
            await _sslStream.AuthenticateAsServerAsync(_cert, false, SslProtocols.Tls12, false);
        }

        /// <summary>
        /// Asynchronously connects to the client described in the <see cref="MqttClientOptions"/>.
        /// </summary>
        /// <param name="options">The <see cref="MqttClientOptions"/> describing the connection.</param>
        public Task ConnectAsync(MqttClientOptions options)
        {
            try
            {
                return Task.Factory.FromAsync(_socket.BeginConnect, _socket.EndConnect, options.Server, options.Port,
                    null);
            }
            catch (SocketException exception)
            {
                throw new MqttCommunicationException(exception);
            }
        }

        /// <summary>
        /// Asynchronously disconnects the client from the server.
        /// </summary>
        public Task DisconnectAsync()
        {
            try
            {
                return Task.Factory.FromAsync(_socket.BeginDisconnect, _socket.EndDisconnect, true, null);
            }
            catch (SocketException exception)
            {
                throw new MqttCommunicationException(exception);
            }
        }

        /// <summary>
        /// Asynchronously writes a sequence of bytes to the socket.
        /// </summary>
        /// <param name="buffer">The buffer to write data from.</param>
        public Task WriteAsync(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            try
            {
                return _sslStream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
                when (ex is SocketException || ex is IOException)
            {
                throw new MqttCommunicationException(ex);
            }
        }

        /// <summary>
        /// Asynchronously reads a sequence of bytes from the socket.
        /// </summary>
        /// <param name="buffer">The buffer to write the data into.</param>
        public Task ReadAsync(byte[] buffer)
        {
            try
            {
                return _sslStream.ReadAsync(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
                when (ex is SocketException || ex is IOException)
            {
                throw new MqttCommunicationException(ex);
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="MqttClientSslChannel"/>.
        /// </summary>
        public void Dispose()
        {
            _sslStream?.Dispose();
            _socket?.Dispose();
        }
    }
}