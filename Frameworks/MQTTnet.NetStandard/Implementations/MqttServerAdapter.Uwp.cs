#if WINDOWS_UWP
using System;
using System.Threading.Tasks;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Server;
using Windows.Networking.Sockets;
using Microsoft.Extensions.Logging;

namespace MQTTnet.Implementations
{
    public class MqttServerAdapter : IMqttServerAdapter, IDisposable
    {
        private readonly ILogger<MqttServerAdapter> _logger;
        private readonly IMqttCommunicationAdapterFactory _communicationAdapterFactory;
        private StreamSocketListener _defaultEndpointSocket;

        public MqttServerAdapter(ILogger<MqttServerAdapter> logger, IMqttCommunicationAdapterFactory communicationAdapterFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _communicationAdapterFactory = communicationAdapterFactory ?? throw new ArgumentNullException(nameof(communicationAdapterFactory));
        }

        public event EventHandler<MqttServerAdapterClientAcceptedEventArgs> ClientAccepted;

        public async Task StartAsync(MqttServerOptions options)
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
                var clientAdapter = _communicationAdapterFactory.CreateServerCommunicationAdapter(new MqttTcpChannel(args.Socket));
                ClientAccepted?.Invoke(this, new MqttServerAdapterClientAcceptedEventArgs(clientAdapter));
            }
            catch (Exception exception)
            {
                _logger.LogError(new EventId(), exception, "Error while accepting connection at default endpoint.");
            }
        }
    }
}
#endif