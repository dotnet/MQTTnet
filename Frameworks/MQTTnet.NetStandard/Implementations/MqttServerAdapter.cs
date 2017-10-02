using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Serializer;
using MQTTnet.Core.Server;

namespace MQTTnet.Implementations
{
    public class MqttServerAdapter : IMqttServerAdapter, IDisposable
    {
        private CancellationTokenSource _cancellationTokenSource;
        private Socket _defaultEndpointSocket;
        private Socket _tlsEndpointSocket;
        private X509Certificate2 _tlsCertificate;

        private bool _isRunning;

        public event Action<IMqttCommunicationAdapter> ClientAccepted;

        public Task StartAsync(MqttServerOptions options)
        {
            if (_isRunning) throw new InvalidOperationException("Server is already started.");

            _isRunning = true;

            _cancellationTokenSource = new CancellationTokenSource();

            if (options.DefaultEndpointOptions.IsEnabled)
            {
                _defaultEndpointSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                _defaultEndpointSocket.Bind(new IPEndPoint(IPAddress.Any, options.GetDefaultEndpointPort()));
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

                _tlsEndpointSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                _tlsEndpointSocket.Bind(new IPEndPoint(IPAddress.Any, options.GetTlsEndpointPort()));
                _tlsEndpointSocket.Listen(options.ConnectionBacklog);

                Task.Run(() => AcceptTlsEndpointConnectionsAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
            }

            return Task.FromResult(0);
        }

        public Task StopAsync()
        {
            _isRunning = false;

            _cancellationTokenSource?.Cancel(false);
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            _defaultEndpointSocket?.Dispose();
            _defaultEndpointSocket = null;

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
                    var clientSocket = await _defaultEndpointSocket.AcceptAsync().ConfigureAwait(false);
                    var clientAdapter = new MqttChannelCommunicationAdapter(new MqttTcpChannel(clientSocket, null), new MqttPacketSerializer());
                    ClientAccepted?.Invoke(clientAdapter);
                }
                catch (Exception exception)
                {
                    MqttTrace.Error(nameof(MqttServerAdapter), exception, "Error while accepting connection at default endpoint.");

                    //excessive CPU consumed if in endless loop of socket errors
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
                    var clientSocket = await _tlsEndpointSocket.AcceptAsync().ConfigureAwait(false);

                    var sslStream = new SslStream(new NetworkStream(clientSocket));
                    await sslStream.AuthenticateAsServerAsync(_tlsCertificate, false, SslProtocols.Tls12, false).ConfigureAwait(false);

                    var clientAdapter = new MqttChannelCommunicationAdapter(new MqttTcpChannel(clientSocket, sslStream), new MqttPacketSerializer());
                    ClientAccepted?.Invoke(clientAdapter);
                }
                catch (Exception exception)
                {
                    MqttTrace.Error(nameof(MqttServerAdapter), exception, "Error while accepting connection at TLS endpoint.");

                    //excessive CPU consumed if in endless loop of socket errors
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}