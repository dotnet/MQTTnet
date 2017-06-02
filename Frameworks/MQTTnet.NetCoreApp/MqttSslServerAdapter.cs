using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Serializer;
using MQTTnet.Core.Server;

namespace MQTTnet
{
    public class MqttSslServerAdapter : IMqttServerAdapter, IDisposable
    {
        private CancellationTokenSource _cancellationTokenSource;
        private Socket _socket;
        private X509Certificate2 _x590Certificate2;

        public event EventHandler<MqttClientConnectedEventArgs> ClientConnected;

        public void Start(MqttServerOptions options)
        {
            if (_socket != null) throw new InvalidOperationException("Server is already started.");

            _cancellationTokenSource = new CancellationTokenSource();

            _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(new IPEndPoint(IPAddress.Any, options.Port));
            _socket.Listen(options.ConnectionBacklog);

            Task.Run(async () => await AcceptConnectionsAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

            _x590Certificate2 = new X509Certificate2(options.CertificatePath);
        }

        public void Stop()
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            _socket?.Dispose();
            _socket = null;
        }

        public void Dispose()
        {
            Stop();
        }

        private async Task AcceptConnectionsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var clientSocket = await _socket.AcceptAsync();

                MqttServerSslChannel mssc = new MqttServerSslChannel(clientSocket, _x590Certificate2);
                await mssc.Authenticate();

                var clientAdapter = new MqttChannelCommunicationAdapter(mssc, new DefaultMqttV311PacketSerializer());
                ClientConnected?.Invoke(this, new MqttClientConnectedEventArgs(clientSocket.RemoteEndPoint.ToString(), clientAdapter));
            }
        }
    }
}