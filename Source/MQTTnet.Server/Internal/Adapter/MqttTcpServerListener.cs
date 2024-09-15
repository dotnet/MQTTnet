// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Formatter;
using MQTTnet.Implementations;
using MQTTnet.Internal;

namespace MQTTnet.Server.Internal.Adapter
{
    public sealed class MqttTcpServerListener : IDisposable
    {
        readonly MqttNetSourceLogger _logger;
        readonly IMqttNetLogger _rootLogger;
        readonly AddressFamily _addressFamily;
        readonly MqttServerOptions _serverOptions;
        readonly MqttServerTcpEndpointBaseOptions _options;
        readonly MqttServerTlsTcpEndpointOptions _tlsOptions;

        CrossPlatformSocket _socket;
        IPEndPoint _localEndPoint;

        public MqttTcpServerListener(
            AddressFamily addressFamily,
            MqttServerOptions serverOptions,
            MqttServerTcpEndpointBaseOptions tcpEndpointOptions,
            IMqttNetLogger logger)
        {
            _addressFamily = addressFamily;
            _serverOptions = serverOptions ?? throw new ArgumentNullException(nameof(serverOptions));
            _options = tcpEndpointOptions ?? throw new ArgumentNullException(nameof(tcpEndpointOptions));
            _rootLogger = logger;
            _logger = logger.WithSource(nameof(MqttTcpServerListener));

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

                _logger.Info("Starting TCP listener (Endpoint={0}, TLS={1})", _localEndPoint, _tlsOptions?.CertificateProvider != null);

                _socket = new CrossPlatformSocket(_addressFamily, ProtocolType.Tcp);

                // Usage of socket options is described here: https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.socket.setsocketoption?view=netcore-2.2
                if (_options.ReuseAddress)
                {
                    _socket.ReuseAddress = true;
                }

                if (_options.NoDelay)
                {
                    _socket.NoDelay = true;
                }

                if (_options.LingerState != null)
                {
                    _socket.LingerState = _options.LingerState;
                }

                if (_options.KeepAlive.HasValue)
                {
                    _socket.KeepAlive = _options.KeepAlive.Value;
                }

                if (_options.TcpKeepAliveInterval.HasValue)
                {
                    _socket.TcpKeepAliveInterval = _options.TcpKeepAliveInterval.Value;
                }

                if (_options.TcpKeepAliveRetryCount.HasValue)
                {
                    _socket.TcpKeepAliveInterval = _options.TcpKeepAliveRetryCount.Value;
                }

                if (_options.TcpKeepAliveTime.HasValue)
                {
                    _socket.TcpKeepAliveTime = _options.TcpKeepAliveTime.Value;
                }

                _socket.Bind(_localEndPoint);

                // Get the local endpoint back from the socket. The port may have changed.
                // This can happen when port 0 is used. Then the OS will choose the next free port.
                _localEndPoint = (IPEndPoint)_socket.LocalEndPoint;
                _options.Port = _localEndPoint.Port;

                _socket.Listen(_options.ConnectionBacklog);

                _logger.Verbose("TCP listener started (Endpoint={0})", _localEndPoint);

                Task.Run(() => AcceptClientConnectionsAsync(cancellationToken), cancellationToken).RunInBackground(_logger);

                return true;
            }
            catch (Exception exception)
            {
                if (!treatErrorsAsWarning)
                {
                    throw;
                }

                _logger.Warning(exception, "Error while starting TCP listener (Endpoint={0})", _localEndPoint);
                return false;
            }
        }

        public void Dispose()
        {
            _socket?.Dispose();
        }

        async Task AcceptClientConnectionsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var clientSocket = await _socket.AcceptAsync(cancellationToken).ConfigureAwait(false);
                    if (clientSocket == null)
                    {
                        continue;
                    }

                    _ = Task.Factory.StartNew(() => TryHandleClientConnectionAsync(clientSocket), cancellationToken, TaskCreationOptions.PreferFairness, TaskScheduler.Default).ConfigureAwait(false);
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

                    _logger.Error(exception, "Error while accepting TCP connection (Endpoint={0})", _localEndPoint);
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

                _logger.Verbose("TCP client '{0}' accepted (Local endpoint={1})", remoteEndPoint, _localEndPoint);

                clientSocket.NoDelay = _options.NoDelay;
                stream = clientSocket.GetStream();
                var clientCertificate = _tlsOptions?.CertificateProvider?.GetCertificate();

                if (clientCertificate != null)
                {
                    if (!clientCertificate.HasPrivateKey)
                    {
                        throw new InvalidOperationException("The certificate for TLS encryption must contain the private key.");
                    }

                    var sslStream = new SslStream(stream, false, _tlsOptions.RemoteCertificateValidationCallback);

                    await sslStream.AuthenticateAsServerAsync(
                        new SslServerAuthenticationOptions
                        {
                            ServerCertificate = clientCertificate,
                            ClientCertificateRequired = _tlsOptions.ClientCertificateRequired,
                            EnabledSslProtocols = _tlsOptions.SslProtocol,
                            CertificateRevocationCheckMode = _tlsOptions.CheckCertificateRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck,
                            EncryptionPolicy = EncryptionPolicy.RequireEncryption,
                            CipherSuitesPolicy = _tlsOptions.CipherSuitesPolicy
                        }).ConfigureAwait(false);

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
                    var tcpChannel = new MqttTcpChannel(stream, remoteEndPoint, clientCertificate);
                    var bufferWriter = new MqttBufferWriter(_serverOptions.WriterBufferSize, _serverOptions.WriterBufferSizeMax);
                    var packetFormatterAdapter = new MqttPacketFormatterAdapter(bufferWriter);

                    using (var clientAdapter = new MqttChannelAdapter(tcpChannel, packetFormatterAdapter, _rootLogger))
                    {
                        clientAdapter.AllowPacketFragmentation = _options.AllowPacketFragmentation;
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

                _logger.Error(exception, "Error while handling TCP client connection");
            }
            finally
            {
                try
                {
                    // ReSharper disable once MethodHasAsyncOverload
                    stream?.Dispose();
                    clientSocket?.Dispose();
                }
                catch (Exception disposeException)
                {
                    _logger.Error(disposeException, "Error while cleaning up client connection");
                }
            }

            _logger.Verbose("TCP client '{0}' disconnected (Local endpoint={1})", remoteEndPoint, _localEndPoint);
        }
    }
}
