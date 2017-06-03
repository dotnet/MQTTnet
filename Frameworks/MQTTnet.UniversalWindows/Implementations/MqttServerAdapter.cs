using System;
using System.Security.Cryptography.X509Certificates;
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
        private StreamSocketListener _sslEndpointSocket;
        private X509Certificate2 _sslCertificate;

        private bool _isRunning;

        public event EventHandler<MqttClientConnectedEventArgs> ClientConnected;

        public void Start(MqttServerOptions options)
        {
            if (_isRunning) throw new InvalidOperationException("Server is already started.");
            _isRunning = true;

            if (options.DefaultEndpointOptions.IsEnabled)
            {
                _defaultEndpointSocket = new StreamSocketListener();
                _defaultEndpointSocket.BindServiceNameAsync(options.GetDefaultEndpointPort().ToString(), SocketProtectionLevel.PlainSocket).GetAwaiter().GetResult();
                _defaultEndpointSocket.ConnectionReceived += AcceptDefaultEndpointConnectionsAsync;                
            }

            if (options.SslEndpointOptions.IsEnabled)
            {
                if (options.SslEndpointOptions.Certificate == null)
                {
                    throw new ArgumentException("SSL certificate is not set.");
                }

                _sslCertificate = new X509Certificate2(options.SslEndpointOptions.Certificate);

                _sslEndpointSocket = new StreamSocketListener();
                _sslEndpointSocket.BindServiceNameAsync(options.GetSslEndpointPort().ToString(), SocketProtectionLevel.Tls12).GetAwaiter().GetResult();
                _sslEndpointSocket.ConnectionReceived += AcceptSslEndpointConnectionsAsync;
            }
        }

        public void Stop()
        {
            _isRunning = false;

            _defaultEndpointSocket?.Dispose();
            _defaultEndpointSocket = null;

            _sslEndpointSocket?.Dispose();
            _sslEndpointSocket = null;
        }

        public void Dispose()
        {
            Stop();
        }

        private void AcceptDefaultEndpointConnectionsAsync(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            try
            {
                var clientAdapter = new MqttChannelCommunicationAdapter(new MqttTcpChannel(args.Socket), new DefaultMqttV311PacketSerializer());
                ClientConnected?.Invoke(this, new MqttClientConnectedEventArgs(args.Socket.Information.RemoteAddress.ToString(), clientAdapter));
            }
            catch (Exception exception)
            {
                MqttTrace.Error(nameof(MqttServerAdapter), exception, "Error while acceping connection at default endpoint.");
            }
        }

        private void AcceptSslEndpointConnectionsAsync(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            try
            {
                var clientAdapter = new MqttChannelCommunicationAdapter(new MqttTcpChannel(args.Socket), new DefaultMqttV311PacketSerializer());
                ClientConnected?.Invoke(this, new MqttClientConnectedEventArgs(args.Socket.Information.RemoteAddress.ToString(), clientAdapter));
            }
            catch (Exception exception)
            {
                MqttTrace.Error(nameof(MqttServerAdapter), exception, "Error while acceping connection at SSL endpoint.");
            }
        }
    }
}