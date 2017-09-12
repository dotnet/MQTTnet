using System;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Security.Cryptography.Certificates;
using Windows.Storage.Streams;
using MQTTnet.Core.Channel;
using MQTTnet.Core.Client;
using MQTTnet.Core.Exceptions;

namespace MQTTnet.Implementations
{
    public sealed class MqttTcpChannel : IMqttCommunicationChannel, IDisposable
    {
        private StreamSocket _socket;

        public MqttTcpChannel()
        {
        }

        public MqttTcpChannel(StreamSocket socket)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
        }

        public async Task ConnectAsync(MqttClientOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            try
            {
                if (_socket == null)
                {
                    _socket = new StreamSocket();
                }

                if (!options.TlsOptions.UseTls)
                {
                    await _socket.ConnectAsync(new HostName(options.Server), options.GetPort().ToString());
                }
                else
                {
                    _socket.Control.ClientCertificate = LoadCertificate(options);

                    if (!options.TlsOptions.CheckCertificateRevocation)
                    {
                        _socket.Control.IgnorableServerCertificateErrors.Add(ChainValidationResult.IncompleteChain);
                        _socket.Control.IgnorableServerCertificateErrors.Add(ChainValidationResult.RevocationInformationMissing);
                    }

                    await _socket.ConnectAsync(new HostName(options.Server), options.GetPort().ToString(), SocketProtectionLevel.Tls12);
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
                await _socket.OutputStream.WriteAsync(buffer.AsBuffer());
                await _socket.OutputStream.FlushAsync();
            }
            catch (SocketException exception)
            {
                throw new MqttCommunicationException(exception);
            }
        }

        public int Peek()
        {
            
        }

        public async Task<ArraySegment<byte>> ReadAsync(int length, byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            try
            {
                var result = await _socket.InputStream.ReadAsync(buffer.AsBuffer(), (uint)buffer.Length, InputStreamOptions.None);
                return new ArraySegment<byte>(buffer, 0, (int)result.Length);
            }
            catch (SocketException exception)
            {
                throw new MqttCommunicationException(exception);
            }
        }

        public void Dispose()
        {
            _socket?.Dispose();
            _socket = null;
        }

        private static Certificate LoadCertificate(MqttClientOptions options)
        {
            if (options.TlsOptions.Certificates == null || !options.TlsOptions.Certificates.Any())
            {
                return null;
            }

            if (options.TlsOptions.Certificates.Count > 1)
            {
                throw new NotSupportedException("Only one client certificate is supported for UWP.");
            }

            return new Certificate(options.TlsOptions.Certificates.First().AsBuffer());
        }
    }
}