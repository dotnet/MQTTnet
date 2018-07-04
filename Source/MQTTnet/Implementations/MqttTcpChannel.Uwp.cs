#if WINDOWS_UWP
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Security.Cryptography.Certificates;
using MQTTnet.Channel;
using MQTTnet.Client;
using MQTTnet.Server;

namespace MQTTnet.Implementations
{
    public class MqttTcpChannel : IMqttChannel
    {
        private readonly MqttClientTcpOptions _options;
        private readonly int _bufferSize;

        private StreamSocket _socket;
        private Stream _readStream;
        private Stream _writeStream;

        public MqttTcpChannel(IMqttClientOptions clientOptions)
        {
            _options = (MqttClientTcpOptions)clientOptions.ChannelOptions;
            _bufferSize = _options.BufferSize;
        }

        public MqttTcpChannel(StreamSocket socket, IMqttServerOptions serverOptions)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            _bufferSize = serverOptions.DefaultEndpointOptions.BufferSize;

            CreateStreams();
        }

        public static Func<MqttClientTcpOptions, IEnumerable<ChainValidationResult>> CustomIgnorableServerCertificateErrorsResolver { get; set; }

        public string Endpoint
        {
            get
            {
                if (_socket?.Information != null)
                {
                    return _socket.Information.RemoteAddress + ":" + _socket.Information.RemotePort;
                }

                return null;
            }
        }

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            if (_socket == null)
            {
                _socket = new StreamSocket();
                _socket.Control.NoDelay = true;
                _socket.Control.KeepAlive = true;
            }

            if (!_options.TlsOptions.UseTls)
            {
                await _socket.ConnectAsync(new HostName(_options.Server), _options.GetPort().ToString());
            }
            else
            {
                _socket.Control.ClientCertificate = LoadCertificate(_options);

                foreach (var ignorableChainValidationResult in ResolveIgnorableServerCertificateErrors())
                {
                    _socket.Control.IgnorableServerCertificateErrors.Add(ignorableChainValidationResult);
                }

                await _socket.ConnectAsync(new HostName(_options.Server), _options.GetPort().ToString(), SocketProtectionLevel.Tls12);
            }

            CreateStreams();
        }

        public Task DisconnectAsync()
        {
            Dispose();
            return Task.FromResult(0);
        }

        public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _readStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // In the write method only the internal buffer will be filled. So here is no
            // async/await required. The real network transmit is done when calling the
            // Flush method.
            _writeStream.Write(buffer, offset, count);
            return _writeStream.FlushAsync(cancellationToken);
        }

        public void Dispose()
        {
            TryDispose(_readStream, () => _readStream = null);
            TryDispose(_writeStream, () => _writeStream = null);
            TryDispose(_socket, () => _socket = null);
        }

        private static Certificate LoadCertificate(IMqttClientChannelOptions options)
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

        private IEnumerable<ChainValidationResult> ResolveIgnorableServerCertificateErrors()
        {
            if (CustomIgnorableServerCertificateErrorsResolver != null)
            {
                return CustomIgnorableServerCertificateErrorsResolver(_options);
            }

            var result = new List<ChainValidationResult>();

            if (_options.TlsOptions.IgnoreCertificateRevocationErrors)
            {
                result.Add(ChainValidationResult.RevocationInformationMissing);
                //_socket.Control.IgnorableServerCertificateErrors.Add(ChainValidationResult.Revoked); Not supported.
                result.Add(ChainValidationResult.RevocationFailure);
            }

            if (_options.TlsOptions.IgnoreCertificateChainErrors)
            {
                result.Add(ChainValidationResult.IncompleteChain);
            }

            if (_options.TlsOptions.AllowUntrustedCertificates)
            {
                result.Add(ChainValidationResult.Untrusted);
            }

            return result;
        }

        private void CreateStreams()
        {
            // Attention! Do not set the buffer for the read method. This will
            // limit the internal buffer and the read operation will hang forever
            // if more data than the buffer size was received.
            _readStream = _socket.InputStream.AsStreamForRead();

            _writeStream = _socket.OutputStream.AsStreamForWrite(_bufferSize);
        }

        private static void TryDispose(IDisposable disposable, Action afterDispose)
        {
            try
            {
                disposable?.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (NullReferenceException)
            {
            }
            finally
            {
                afterDispose();
            }
        }
    }
}
#endif