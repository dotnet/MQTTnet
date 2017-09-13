using System;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Serializer;
using MQTTnet.Core.Server;
using Windows.Networking.Sockets;

namespace MQTTnet.Implementations
{
    public class MqttServerAdapter : IMqttServerAdapter, IDisposable
    {
        private StreamSocketListener _defaultEndpointSocket;

        private bool _isRunning;

        public event EventHandler<MqttClientConnectedEventArgs> ClientConnected;

        public void Start(MqttServerOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (_isRunning) throw new InvalidOperationException("Server is already started.");

            _isRunning = true;

            if (options.DefaultEndpointOptions.IsEnabled)
            {
                _defaultEndpointSocket = new StreamSocketListener();
                _defaultEndpointSocket.BindServiceNameAsync(options.GetDefaultEndpointPort().ToString(), SocketProtectionLevel.PlainSocket).GetAwaiter().GetResult();
                _defaultEndpointSocket.ConnectionReceived += AcceptDefaultEndpointConnectionsAsync;
            }

            if (options.TlsEndpointOptions.IsEnabled)
            {
                throw new NotSupportedException("TLS servers are not supported for UWP apps.");
            }
        }

        public void Stop()
        {
            _isRunning = false;

            _defaultEndpointSocket?.Dispose();
            _defaultEndpointSocket = null;
        }

        public void Dispose()
        {
            Stop();
        }

        private void AcceptDefaultEndpointConnectionsAsync(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            try
            {
                var clientAdapter = new MqttChannelCommunicationAdapter(new MqttTcpChannel(args.Socket), new MqttPacketSerializer());
                ClientConnected?.Invoke(this, new MqttClientConnectedEventArgs(args.Socket.Information.RemoteAddress.ToString(), clientAdapter));
            }
            catch (Exception exception)
            {
                MqttTrace.Error(nameof(MqttServerAdapter), exception, "Error while accepting connection at default endpoint.");
            }
        }
    }
}