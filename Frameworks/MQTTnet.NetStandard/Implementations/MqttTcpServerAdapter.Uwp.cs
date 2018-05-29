#if WINDOWS_UWP
using System;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Serializer;
using MQTTnet.Server;

namespace MQTTnet.Implementations
{
    public class MqttTcpServerAdapter : IMqttServerAdapter, IDisposable
    {
        private readonly IMqttNetLogger _logger;
        private StreamSocketListener _defaultEndpointSocket;

        public MqttTcpServerAdapter(IMqttNetLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public event EventHandler<MqttServerAdapterClientAcceptedEventArgs> ClientAccepted;

        public async Task StartAsync(IMqttServerOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (_defaultEndpointSocket != null) throw new InvalidOperationException("Server is already started.");
            
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
            if (_defaultEndpointSocket != null)
            {
                _defaultEndpointSocket.ConnectionReceived -= AcceptDefaultEndpointConnectionsAsync;
            }

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
                var clientAdapter = new MqttChannelAdapter(new MqttTcpChannel(args.Socket), new MqttPacketSerializer(), _logger);
                ClientAccepted?.Invoke(this, new MqttServerAdapterClientAcceptedEventArgs(clientAdapter));
            }
            catch (Exception exception)
            {
                _logger.Error<MqttTcpServerAdapter>(exception, "Error while accepting connection at default endpoint.");
            }
        }
    }
}
#endif