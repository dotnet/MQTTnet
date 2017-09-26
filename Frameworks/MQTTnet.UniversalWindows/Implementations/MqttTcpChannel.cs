using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Security.Cryptography.Certificates;
using MQTTnet.Core.Channel;
using MQTTnet.Core.Client;

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
            CreateStreams();
        }

        public Stream SendStream { get; private set; }
        public Stream ReceiveStream { get; private set; }
        public Stream RawReceiveStream { get; private set; }

        public async Task ConnectAsync(MqttClientOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

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

            CreateStreams();
        }

        public Task DisconnectAsync()
        {
            Dispose();
            return Task.FromResult(0);
        }

        public void Dispose()
        {
            RawReceiveStream?.Dispose();
            RawReceiveStream = null;

            SendStream?.Dispose();
            SendStream = null;

            ReceiveStream?.Dispose();
            ReceiveStream = null;

            _socket?.Dispose();
            _socket = null;
        }

        private void CreateStreams()
        {
            SendStream = _socket.OutputStream.AsStreamForWrite();
            ReceiveStream = _socket.InputStream.AsStreamForRead();
            RawReceiveStream = ReceiveStream;
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