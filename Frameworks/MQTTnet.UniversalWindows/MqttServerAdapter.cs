using System;
using System.Threading;
using Windows.Networking.Sockets;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Serializer;
using MQTTnet.Core.Server;

namespace MQTTnet
{
    public sealed class MqttServerAdapter : IMqttServerAdapter, IDisposable
    {
        private CancellationTokenSource _cancellationTokenSource;
        private StreamSocketListener _socket;

        public event EventHandler<MqttClientConnectedEventArgs> ClientConnected;

        public void Start(MqttServerOptions options)
        {
            if (_socket != null) throw new InvalidOperationException("Server is already started.");

            _cancellationTokenSource = new CancellationTokenSource();

            _socket = new StreamSocketListener();
            _socket.BindServiceNameAsync(options.Port.ToString()).AsTask().Wait();
            _socket.ConnectionReceived += ConnectionReceived;
        }

        private void ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            var clientAdapter = new MqttChannelCommunicationAdapter(new MqttTcpChannel(args.Socket), new DefaultMqttV311PacketSerializer());

            var identifier = $"{args.Socket.Information.RemoteAddress}:{args.Socket.Information.RemotePort}";
            ClientConnected?.Invoke(this, new MqttClientConnectedEventArgs(identifier, clientAdapter));
        }

        public void Stop()
        {
            _cancellationTokenSource?.Dispose();

            if (_socket != null)
            {
                _socket.ConnectionReceived -= ConnectionReceived;
            }

            _socket?.Dispose();
            _socket = null;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}