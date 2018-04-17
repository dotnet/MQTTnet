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
    public sealed class MqttTcpChannel : IMqttChannel
    {
#if NET452 || NET461 || NETSTANDARD2_0
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        public static int BufferSize { get; set; } = 4096 * 20; // Can be changed for fine tuning by library user.

        private readonly int _bufferSize = BufferSize;
#else
        private readonly int _bufferSize = 0;
#endif

        private readonly MqttClientTcpOptions _options;

        private Socket _socket;
        private SslStream _sslStream;

        /// <summary>
        /// called on client sockets are created in connect
        /// </summary>
        public MqttTcpChannel(MqttClientTcpOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _bufferSize = options.BufferSize;
        }

        /// <summary>
        /// called on server, sockets are passed in
        /// connect will not be called
        /// </summary>
        public MqttTcpChannel(Socket socket, SslStream sslStream)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            _sslStream = sslStream;

            CreateStreams();
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

#if NET452 || NET461
            await Task.Factory.FromAsync(_socket.BeginConnect, _socket.EndConnect, _options.Server, _options.GetPort(), null).ConfigureAwait(false);
#else
            await _socket.ConnectAsync(_options.Server, _options.GetPort()).ConfigureAwait(false);
#endif

            _socket.NoDelay = true;

            if (_options.TlsOptions.UseTls)
            {
                _sslStream = new SslStream(new NetworkStream(_socket, true), false, InternalUserCertificateValidationCallback);
                await _sslStream.AuthenticateAsClientAsync(_options.Server, LoadCertificates(), SslProtocols.Tls12, _options.TlsOptions.IgnoreCertificateRevocationErrors).ConfigureAwait(false);
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
            var oneStreamIsUsed = SendStream != null && ReceiveStream != null && ReferenceEquals(SendStream, ReceiveStream);

            try
            {
                SendStream?.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (NullReferenceException)
            {
            }
            finally
            {
                SendStream = null;
            }

            try
            {
                if (!oneStreamIsUsed)
                {
                    ReceiveStream?.Dispose();
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (NullReferenceException)
            {
            }
            finally
            {
                ReceiveStream = null;
            }

            try
            {
                _sslStream?.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (NullReferenceException)
            {
            }
            finally
            {
                _sslStream = null;
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

        private void CreateStreams()
        {
            Stream stream;
            if (_sslStream != null)
            {
                stream = _sslStream;
            }
            else
            {
                stream = new NetworkStream(_socket, true);
            }

#if NET452 || NET461 || NETSTANDARD2_0
            SendStream = new BufferedStream(stream, _bufferSize);
            ReceiveStream = new BufferedStream(stream, _bufferSize);
#else
            SendStream = stream;
            ReceiveStream = stream;
#endif
        }
    }
}
#endif
