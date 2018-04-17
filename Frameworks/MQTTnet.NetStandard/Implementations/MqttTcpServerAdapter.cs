﻿#if NET452 || NET461 || NETSTANDARD1_3 || NETSTANDARD2_0
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
    public class MqttTcpServerAdapter : IMqttServerAdapter, IDisposable
    {
        private readonly IMqttNetLogger _logger;

        private CancellationTokenSource _cancellationTokenSource;
        private Socket _defaultEndpointSocket;
        private Socket _tlsEndpointSocket;
        private X509Certificate2 _tlsCertificate;

        public MqttTcpServerAdapter(IMqttNetLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public event EventHandler<MqttServerAdapterClientAcceptedEventArgs> ClientAccepted;

        public Task StartAsync(IMqttServerOptions options)
        {
            if (_cancellationTokenSource != null) throw new InvalidOperationException("Server is already started.");

            _cancellationTokenSource = new CancellationTokenSource();

            if (options.DefaultEndpointOptions.IsEnabled)
            {
                _defaultEndpointSocket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

                _defaultEndpointSocket.Bind(new IPEndPoint(options.DefaultEndpointOptions.BoundIPAddress, options.GetDefaultEndpointPort()));
                _defaultEndpointSocket.Listen(options.ConnectionBacklog);

                Task.Run(() => AcceptDefaultEndpointConnectionsAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
            }

            if (options.TlsEndpointOptions.IsEnabled)
            {
                if (options.TlsEndpointOptions.Certificate == null)
                {
                    throw new ArgumentException("TLS certificate is not set.");
                }

                _tlsCertificate = new X509Certificate2(options.TlsEndpointOptions.Certificate);
                if (!_tlsCertificate.HasPrivateKey)
                {
                    throw new InvalidOperationException("The certificate for TLS encryption must contain the private key.");
                }

                _tlsEndpointSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                _tlsEndpointSocket.Bind(new IPEndPoint(options.TlsEndpointOptions.BoundIPAddress, options.GetTlsEndpointPort()));
                _tlsEndpointSocket.Listen(options.ConnectionBacklog);

                Task.Run(() => AcceptTlsEndpointConnectionsAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
            }

            return Task.FromResult(0);
        }

        public Task StopAsync()
        {
            _cancellationTokenSource?.Cancel(false);
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            _defaultEndpointSocket?.Dispose();
            _defaultEndpointSocket = null;

            _tlsCertificate = null;

            _tlsEndpointSocket?.Dispose();
            _tlsEndpointSocket = null;

            return Task.FromResult(0);
        }

        public void Dispose()
        {
            StopAsync();
        }

        private async Task AcceptDefaultEndpointConnectionsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    //todo: else branch can be used with min dependency NET46
#if NET452 || NET461
                    var clientSocket = await Task.Factory.FromAsync(_defaultEndpointSocket.BeginAccept, _defaultEndpointSocket.EndAccept, null).ConfigureAwait(false);
#else
                    var clientSocket = await _defaultEndpointSocket.AcceptAsync().ConfigureAwait(false);
#endif
                    clientSocket.NoDelay = true;

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

                    _logger.Error<MqttTcpServerAdapter>(exception, "Error while accepting connection at default endpoint.");
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task AcceptTlsEndpointConnectionsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
#if NET452 || NET461
                    var clientSocket = await Task.Factory.FromAsync(_tlsEndpointSocket.BeginAccept, _tlsEndpointSocket.EndAccept, null).ConfigureAwait(false);
#else
                    var clientSocket = await _tlsEndpointSocket.AcceptAsync().ConfigureAwait(false);
#endif

                    var sslStream = new SslStream(new NetworkStream(clientSocket));
                    await sslStream.AuthenticateAsServerAsync(_tlsCertificate, false, SslProtocols.Tls12, false).ConfigureAwait(false);

                    var clientAdapter = new MqttChannelAdapter(new MqttTcpChannel(clientSocket, sslStream), new MqttPacketSerializer(), _logger);
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

                    _logger.Error<MqttTcpServerAdapter>(exception, "Error while accepting connection at TLS endpoint.");
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
#endif