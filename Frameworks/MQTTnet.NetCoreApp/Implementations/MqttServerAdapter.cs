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
        private Socket _sslEndpointSocket;
        private X509Certificate2 _sslCertificate;

        private bool _isRunning;

        public event EventHandler<MqttClientConnectedEventArgs> ClientConnected;

        public void Start(MqttServerOptions options)
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

            if (options.SslEndpointOptions.IsEnabled)
            {
                if (options.SslEndpointOptions.Certificate == null)
                {
                    throw new ArgumentException("SSL certificate is not set.");
                }

                _sslCertificate = new X509Certificate2(options.SslEndpointOptions.Certificate);

                _sslEndpointSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                _sslEndpointSocket.Bind(new IPEndPoint(IPAddress.Any, options.GetSslEndpointPort()));
                _sslEndpointSocket.Listen(options.ConnectionBacklog);

                Task.Run(() => AcceptSslEndpointConnectionsAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
            }
        }

        public void Stop()
        {
            _isRunning = false;

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            _defaultEndpointSocket?.Dispose();
            _defaultEndpointSocket = null;

            _sslEndpointSocket?.Dispose();
            _sslEndpointSocket = null;
        }

        public void Dispose()
        {
            Stop();
        }

        private async Task AcceptDefaultEndpointConnectionsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var clientSocket = await _defaultEndpointSocket.AcceptAsync();
                    var clientAdapter = new MqttChannelCommunicationAdapter(new MqttTcpChannel(clientSocket, null), new DefaultMqttV311PacketSerializer());
                    ClientConnected?.Invoke(this, new MqttClientConnectedEventArgs(clientSocket.RemoteEndPoint.ToString(), clientAdapter));
                }
                catch (Exception exception)
                {
                    MqttTrace.Error(nameof(MqttServerAdapter), exception, "Error while acceping connection at default endpoint.");
                }
            }
        }

        private async Task AcceptSslEndpointConnectionsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var clientSocket = await _defaultEndpointSocket.AcceptAsync();

                    var sslStream = new SslStream(new NetworkStream(clientSocket));
                    await sslStream.AuthenticateAsServerAsync(_sslCertificate, false, SslProtocols.Tls12, false);
                    
                    var clientAdapter = new MqttChannelCommunicationAdapter(new MqttTcpChannel(clientSocket, sslStream), new DefaultMqttV311PacketSerializer());
                    ClientConnected?.Invoke(this, new MqttClientConnectedEventArgs(clientSocket.RemoteEndPoint.ToString(), clientAdapter));
                }
                catch (Exception exception)
                {
                    MqttTrace.Error(nameof(MqttServerAdapter), exception, "Error while acceping connection at SSL endpoint.");
                }
            }
        }
    }
}