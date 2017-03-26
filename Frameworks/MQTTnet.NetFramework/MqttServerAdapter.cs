using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Serializer;
using MQTTnet.Core.Server;

namespace MQTTnet
{
    public sealed class MqttServerAdapter : IMqttServerAdapter, IDisposable
    {
        private CancellationTokenSource _cancellationTokenSource;
        private Socket _socket;

        public event EventHandler<MqttClientConnectedEventArgs> ClientConnected;

        public void Start(MqttServerOptions options)
        {
            if (_socket != null) throw new InvalidOperationException("Server is already started.");

            _cancellationTokenSource = new CancellationTokenSource();

            _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(new IPEndPoint(IPAddress.Any, options.Port));
            _socket.Listen(options.ConnectionBacklog);
            
            Task.Run(async () => await AcceptConnectionsAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
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
                var clientSocket = await Task.Factory.FromAsync(_socket.BeginAccept, _socket.EndAccept, null);
                var clientAdapter = new MqttChannelCommunicationAdapter(new MqttTcpChannel(clientSocket), new DefaultMqttV311PacketSerializer());    
                ClientConnected?.Invoke(this, new MqttClientConnectedEventArgs(clientSocket.RemoteEndPoint.ToString(), clientAdapter));
            }
        }
    }
}