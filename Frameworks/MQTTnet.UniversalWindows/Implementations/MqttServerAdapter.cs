using System;
using System.Threading.Tasks;
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

        public event Action<IMqttCommunicationAdapter> ClientAccepted;

        public async Task StartAsync(MqttServerOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (_isRunning) throw new InvalidOperationException("Server is already started.");

            _isRunning = true;

            if (options.DefaultEndpointOptions.IsEnabled)
            {
                _defaultEndpointSocket = new StreamSocketListener();
                await _defaultEndpointSocket.BindServiceNameAsync(options.GetDefaultEndpointPort().ToString(), SocketProtectionLevel.PlainSocket);
                _defaultEndpointSocket.ConnectionReceived += AcceptDefaultEndpointConnectionsAsync;
            }

            if (options.TlsEndpointOptions.IsEnabled)
            {
                throw new NotSupportedException("TLS servers are not supported for UWP apps.");
            }
        }

        public Task StopAsync()
        {
            _isRunning = false;

            _defaultEndpointSocket?.Dispose();
            _defaultEndpointSocket = null;

            return Task.FromResult(0);
        }

        public void Dispose()
        {
            StopAsync();
        }

        private void AcceptDefaultEndpointConnectionsAsync(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            try
            {
                var clientAdapter = new MqttChannelCommunicationAdapter(new MqttTcpChannel(args.Socket), new MqttPacketSerializer());
                ClientAccepted?.Invoke(clientAdapter);
            }
            catch (Exception exception)
            {
                MqttNetTrace.Error(nameof(MqttServerAdapter), exception, "Error while accepting connection at default endpoint.");
            }
        }
    }
}