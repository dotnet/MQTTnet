using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using MQTTnet.Core.Channel;
using MQTTnet.Core.Client;
using MQTTnet.Core.Exceptions;

namespace MQTTnet.Implementations
{
    public class MqttTcpChannel : IMqttCommunicationChannel, IDisposable
    {
        private readonly Socket _socket;
        private SslStream _sslStream;

        public MqttTcpChannel()
        {
            _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        }

        public MqttTcpChannel(Socket socket, SslStream sslStream)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            _sslStream = sslStream;
        }

        public async Task ConnectAsync(MqttClientOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            try
            {
                await Task.Factory.FromAsync(_socket.BeginConnect, _socket.EndConnect, options.Server, options.GetPort(), null);

                if (options.SslOptions.UseSsl)
                {
                    _sslStream = new SslStream(new NetworkStream(_socket, true));
                    await _sslStream.AuthenticateAsClientAsync(options.Server, LoadCertificates(options), SslProtocols.Tls12, options.SslOptions.CheckCertificateRevocation);
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
                _sslStream.Dispose();
                _socket.Dispose();
                return Task.FromResult(0);
            }
            catch (SocketException exception)
            {
                throw new MqttCommunicationException(exception);
            }
        }

        public Task WriteAsync(byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            try
            {
                if (_sslStream != null)
                {
                    return _sslStream.WriteAsync(buffer, 0, buffer.Length);
                }

                return Task.Factory.FromAsync(
                    // ReSharper disable once AssignNullToNotNullAttribute
                    _socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, null, null),
                    _socket.EndSend);

            }
            catch (SocketException exception)
            {
                throw new MqttCommunicationException(exception);
            }
        }

        public Task ReadAsync(byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            try
            {
                if (_sslStream != null)
                {
                    return _sslStream.ReadAsync(buffer, 0, buffer.Length);
                }

                return Task.Factory.FromAsync(
                    // ReSharper disable once AssignNullToNotNullAttribute
                    _socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, null, null),
                    _socket.EndReceive);
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
        }

        private static X509CertificateCollection LoadCertificates(MqttClientOptions options)
        {
            var certificates = new X509CertificateCollection();
            if (options.SslOptions.Certificates == null)
            {
                return certificates;
            }

            foreach (var certificate in options.SslOptions.Certificates)
            {
                certificates.Add(new X509Certificate(certificate));
            }

            return certificates;
        }
    }
}