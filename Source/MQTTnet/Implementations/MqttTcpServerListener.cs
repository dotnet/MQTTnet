#if !WINDOWS_UWP
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Formatter;
using MQTTnet.Internal;
using MQTTnet.Server;
using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Implementations
{
    public sealed class MqttTcpServerListener : IDisposable
    {
        readonly IMqttNetScopedLogger _logger;
        readonly IMqttNetLogger _rootLogger;
        readonly AddressFamily _addressFamily;
        readonly MqttServerTcpEndpointBaseOptions _options;
        readonly MqttServerTlsTcpEndpointOptions _tlsOptions;
        readonly X509Certificate2 _tlsCertificate;

        CrossPlatformSocket _socket;
        IPEndPoint _localEndPoint;

        public MqttTcpServerListener(
            AddressFamily addressFamily,
            MqttServerTcpEndpointBaseOptions options,
            X509Certificate2 tlsCertificate,
            IMqttNetLogger logger)
        {
            _addressFamily = addressFamily;
            _options = options;
            _tlsCertificate = tlsCertificate;
            _rootLogger = logger;
            _logger = logger.CreateScopedLogger(nameof(MqttTcpServerListener));

            if (_options is MqttServerTlsTcpEndpointOptions tlsOptions)
            {
                _tlsOptions = tlsOptions;
            }
        }

        public Func<IMqttChannelAdapter, Task> ClientHandler { get; set; }

        public bool Start(bool treatErrorsAsWarning, CancellationToken cancellationToken)
        {
            try
            {
                var boundIp = _options.BoundInterNetworkAddress;
                if (_addressFamily == AddressFamily.InterNetworkV6)
                {
                    boundIp = _options.BoundInterNetworkV6Address;
                }

                _localEndPoint = new IPEndPoint(boundIp, _options.Port);

                _logger.Info("Starting TCP listener for {0} TLS={1}.", _localEndPoint, _tlsCertificate != null);

                _socket = new CrossPlatformSocket(_addressFamily);

                // Usage of socket options is described here: https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.socket.setsocketoption?view=netcore-2.2

                if (_options.ReuseAddress)
                {
                    _socket.ReuseAddress = true;
                }

                if (_options.NoDelay)
                {
                    _socket.NoDelay = true;
                }

                _socket.Bind(_localEndPoint);
                _socket.Listen(_options.ConnectionBacklog);
                
                Task.Run(() => AcceptClientConnectionsAsync(cancellationToken), cancellationToken).Forget(_logger);

                return true;
            }
            catch (Exception exception)
            {
                if (!treatErrorsAsWarning)
                {
                    throw;
                }

                _logger.Warning(exception, "Error while creating listener socket for local end point '{0}'.", _localEndPoint);
                return false;
            }
        }

        public void Dispose()
        {
            _socket?.Dispose();

#if NETSTANDARD1_3 || NETSTANDARD2_0 || NET461 || NET472
            _tlsCertificate?.Dispose();
#endif
        }

        async Task AcceptClientConnectionsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var clientSocket = await _socket.AcceptAsync().ConfigureAwait(false);
                    if (clientSocket == null)
                    {
                        continue;
                    }

                    Task.Run(() => TryHandleClientConnectionAsync(clientSocket), cancellationToken).Forget(_logger);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception exception)
                {
                    if (exception is SocketException socketException)
                    {
                        if (socketException.SocketErrorCode == SocketError.ConnectionAborted ||
                            socketException.SocketErrorCode == SocketError.OperationAborted)
                        {
                            continue;
                        }
                    }

                    _logger.Error(exception, "Error while accepting connection at TCP listener {0} TLS={1}.", _localEndPoint, _tlsCertificate != null);
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        async Task TryHandleClientConnectionAsync(CrossPlatformSocket clientSocket)
        {
            Stream stream = null;
            string remoteEndPoint = null;

            try
            {
                remoteEndPoint = clientSocket.RemoteEndPoint.ToString();

                _logger.Verbose("Client '{0}' accepted by TCP listener '{1}, {2}'.",
                    remoteEndPoint,
                    _localEndPoint,
                    _addressFamily == AddressFamily.InterNetwork ? "ipv4" : "ipv6");

                clientSocket.NoDelay = _options.NoDelay;
                stream = clientSocket.GetStream();
                X509Certificate2 clientCertificate = null;

                if (_tlsCertificate != null)
                {
                    var sslStream = new SslStream(stream, false, _tlsOptions.RemoteCertificateValidationCallback);

                    await sslStream.AuthenticateAsServerAsync(
                        _tlsCertificate,
                        _tlsOptions.ClientCertificateRequired,
                        _tlsOptions.SslProtocol,
                        _tlsOptions.CheckCertificateRevocation).ConfigureAwait(false);

                    stream = sslStream;

                    clientCertificate = sslStream.RemoteCertificate as X509Certificate2;

                    if (clientCertificate == null && sslStream.RemoteCertificate != null)
                    {
                        clientCertificate = new X509Certificate2(sslStream.RemoteCertificate.Export(X509ContentType.Cert));
                    }
                }

                var clientHandler = ClientHandler;
                if (clientHandler != null)
                {
                    using (var clientAdapter = new MqttChannelAdapter(new MqttTcpChannel(stream, remoteEndPoint, clientCertificate), new MqttPacketFormatterAdapter(new MqttPacketWriter()), _rootLogger))
                    {
                        await clientHandler(clientAdapter).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception exception)
            {
                if (exception is ObjectDisposedException)
                {
                    // It can happen that the listener socket is accessed after the cancellation token is already set and the listener socket is disposed.
                    return;
                }

                if (exception is SocketException socketException &&
                    socketException.SocketErrorCode == SocketError.OperationAborted)
                {
                    return;
                }

                _logger.Error(exception, "Error while handling client connection.");
            }
            finally
            {
                try
                {
                    stream?.Dispose();
                    clientSocket?.Dispose();
                }
                catch (Exception disposeException)
                {
                    _logger.Error(disposeException, "Error while cleaning up client connection");
                }
            }

            _logger.Verbose("Client '{0}' disconnected at TCP listener '{1}, {2}'.",
                remoteEndPoint,
                _localEndPoint,
                _addressFamily == AddressFamily.InterNetwork ? "ipv4" : "ipv6");
        }
    }
}
#endif