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
        private readonly MqttNetTrace _trace;
        private StreamSocketListener _defaultEndpointSocket;

        public MqttServerAdapter(MqttNetTrace trace)
        {
            _trace = trace ?? throw new ArgumentNullException(nameof(trace));
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
                var clientAdapter = new MqttChannelCommunicationAdapter(new MqttTcpChannel(args.Socket), new MqttPacketSerializer(), _trace);
                ClientAccepted?.Invoke(this, new MqttServerAdapterClientAcceptedEventArgs(clientAdapter));
            }
            catch (Exception exception)
            {
                _trace.Error(nameof(MqttServerAdapter), exception, "Error while accepting connection at default endpoint.");
            }
        }
    }
}