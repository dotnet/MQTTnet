#if NET452 || NET461 || NETSTANDARD1_3 || NETSTANDARD2_0
using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Serializer;
using MQTTnet.Server;

namespace MQTTnet.Implementations
{
    public class MqttTcpServerListener : IDisposable
    {
        private readonly IMqttNetChildLogger _logger;
        private readonly CancellationToken _cancellationToken;
        private readonly AddressFamily _addressFamily;
        private readonly MqttServerTcpEndpointBaseOptions _options;
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
        }

        public event EventHandler<MqttServerAdapterClientAcceptedEventArgs> ClientAccepted;

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
            Task.Run(AcceptClientConnectionsAsync, _cancellationToken);
        }

        private async Task AcceptClientConnectionsAsync()
        {
            while (!_cancellationToken.IsCancellationRequested)
            {
                try
                {
#if NET452 || NET461
                    var clientSocket = await Task.Factory.FromAsync(_socket.BeginAccept, _socket.EndAccept, null).ConfigureAwait(false);
#else
                    var clientSocket = await _socket.AcceptAsync().ConfigureAwait(false);
#endif
                    clientSocket.NoDelay = true;

                    if (_tlsCertificate != null)
                    {
                        var sslStream = new SslStream(new NetworkStream(clientSocket), false);
                        await sslStream.AuthenticateAsServerAsync(_tlsCertificate, false, SslProtocols.Tls12, false).ConfigureAwait(false);
                    }

                    _logger.Verbose($"Client '{clientSocket.RemoteEndPoint}' accepted by TCP listener '{_socket.LocalEndPoint}, {_addressFamily}'.");

                    var clientAdapter = new MqttChannelAdapter(new MqttTcpChannel(clientSocket, null), new MqttPacketSerializer(), _logger);
                    ClientAccepted?.Invoke(this, new MqttServerAdapterClientAcceptedEventArgs(clientAdapter));
                }
                catch (ObjectDisposedException)
                {
                    // It can happen that the listener socket is accessed after the cancellation token is already set and the listener socket is disposed.
                }
                catch (Exception exception)
                {
                    if (exception is SocketException s && s.SocketErrorCode == SocketError.OperationAborted)
                    {
                        return;
                    }

                    _logger.Error(exception, $"Error while accepting connection at TCP listener {_socket.LocalEndPoint} TLS={_tlsCertificate != null}.");
                    await Task.Delay(TimeSpan.FromSeconds(1), _cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public void Dispose()
        {
            _socket?.Dispose();

#if NETSTANDARD1_3 || NETSTANDARD2_0 || NET461
            _tlsCertificate?.Dispose();
#endif
        }
    }
}
#endif