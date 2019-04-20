#if !WINDOWS_UWP
using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Formatter;
using MQTTnet.Server;

namespace MQTTnet.Implementations
{
    public class MqttTcpServerListener : IDisposable
    {
        private readonly IMqttNetChildLogger _logger;
        private readonly CancellationToken _cancellationToken;
        private readonly AddressFamily _addressFamily;
        private readonly MqttServerTcpEndpointBaseOptions _options;
        private readonly MqttServerTlsTcpEndpointOptions _tlsOptions;
        private readonly X509Certificate2 _tlsCertificate;
        private Socket _socket;

        public MqttTcpServerListener(
            AddressFamily addressFamily,
            MqttServerTcpEndpointBaseOptions options,
            X509Certificate2 tlsCertificate,
            CancellationToken cancellationToken,
            IMqttNetChildLogger logger)
        {
            _addressFamily = addressFamily;
            _options = options;
            _tlsCertificate = tlsCertificate;
            _cancellationToken = cancellationToken;
            _logger = logger.CreateChildLogger(nameof(MqttTcpServerListener));

            if (_options is MqttServerTlsTcpEndpointOptions tlsOptions)
            {
                _tlsOptions = tlsOptions;
            }
        }

        public Action<MqttServerAdapterClientAcceptedEventArgs> ClientAcceptedHandler { get; set; }

        public void Start()
        {
            var boundIp = _options.BoundInterNetworkAddress;
            if (_addressFamily == AddressFamily.InterNetworkV6)
            {
                boundIp = _options.BoundInterNetworkV6Address;
            }

            _socket = new Socket(_addressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(new IPEndPoint(boundIp, _options.Port));

            _logger.Info($"Starting TCP listener for {_socket.LocalEndPoint} TLS={_tlsCertificate != null}.");

            _socket.Listen(_options.ConnectionBacklog);
            Task.Run(() => AcceptClientConnectionsAsync(_cancellationToken), _cancellationToken);
        }

        public void Dispose()
        {
            _socket?.Dispose();

#if NETSTANDARD1_3 || NETSTANDARD2_0 || NET461 || NET472
            _tlsCertificate?.Dispose();
#endif
        }

        private async Task AcceptClientConnectionsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
#if NET452 || NET461
                    var clientSocket = await Task.Factory.FromAsync(_socket.BeginAccept, _socket.EndAccept, null).ConfigureAwait(false);
#else
                    var clientSocket = await _socket.AcceptAsync().ConfigureAwait(false);
#endif
#pragma warning disable 4014
                    Task.Run(() => TryHandleClientConnectionAsync(clientSocket), cancellationToken);
#pragma warning restore 4014
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, $"Error while accepting connection at TCP listener {_socket.LocalEndPoint} TLS={_tlsCertificate != null}.");
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task TryHandleClientConnectionAsync(Socket clientSocket)
        {
            Stream stream = null;

            try
            {
                _logger.Verbose("Client '{0}' accepted by TCP listener '{1}, {2}'.",
                    clientSocket.RemoteEndPoint,
                    _socket.LocalEndPoint,
                    _addressFamily == AddressFamily.InterNetwork ? "ipv4" : "ipv6");

                clientSocket.NoDelay = _options.NoDelay;

                stream = new NetworkStream(clientSocket, true);

                if (_tlsCertificate != null)
                {
                    var sslStream = new SslStream(stream, false);
                    await sslStream.AuthenticateAsServerAsync(_tlsCertificate, false, _tlsOptions.SslProtocol, false).ConfigureAwait(false);
                    stream = sslStream;
                }

                var clientAdapter = new MqttChannelAdapter(new MqttTcpChannel(stream), new MqttPacketFormatterAdapter(), _logger);
                ClientAcceptedHandler?.Invoke(new MqttServerAdapterClientAcceptedEventArgs(clientAdapter));
            }
            catch (Exception exception)
            {
                if (exception is ObjectDisposedException)
                {
                    // It can happen that the listener socket is accessed after the cancellation token is already set and the listener socket is disposed.
                    return;
                }

                if (exception is SocketException socketException && socketException.SocketErrorCode == SocketError.OperationAborted)
                {
                    return;
                }

                try
                {
                    // Dispose already allocated resources.
                    stream?.Dispose();
                    clientSocket?.Dispose();
                }
                catch (Exception disposeException)
                {
                    _logger.Error(disposeException, "Error while cleanup of broken connection.");
                }
            }
        }
    }
}
#endif