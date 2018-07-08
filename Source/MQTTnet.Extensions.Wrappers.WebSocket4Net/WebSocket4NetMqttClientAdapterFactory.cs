using MQTTnet.Client;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Channel;
using MQTTnet.Diagnostics;
using MQTTnet.Serializer;
using WebSocket4Net;

namespace MQTTnet.TestApp.NetCore
{
    public class WebSocket4NetMqttClientAdapterFactory : IMqttClientAdapterFactory
    {
        public IMqttChannelAdapter CreateClientAdapter(IMqttClientOptions options, IMqttNetChildLogger logger)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            if (!(options.ChannelOptions is MqttClientWebSocketOptions))
            {
                throw new NotSupportedException("Only WebSocket connections are supported.");
            }

            return new MqttChannelAdapter(new WebSocket4NetMqttChannel(options), new MqttPacketSerializer(), logger);
        }

        private class WebSocket4NetMqttChannel : IMqttChannel
        {
            private readonly BlockingCollection<byte> _receiveBuffer = new BlockingCollection<byte>();

            private readonly IMqttClientOptions _clientOptions;
            private WebSocket4Net.WebSocket _webSocket;

            public WebSocket4NetMqttChannel(IMqttClientOptions clientOptions)
            {
                _clientOptions = clientOptions ?? throw new ArgumentNullException(nameof(clientOptions));
            }

            public string Endpoint { get; } = "";

            public Task ConnectAsync(CancellationToken cancellationToken)
            {
                var channelOptions = (MqttClientWebSocketOptions)_clientOptions.ChannelOptions;

                var uri = "ws://" + channelOptions.Uri;
                var sslProtocols = SslProtocols.None;

                if (channelOptions.TlsOptions.UseTls)
                {
                    uri = "wss://" + channelOptions.Uri;
                    sslProtocols = SslProtocols.Tls12;
                }

                var subProtocol = channelOptions.SubProtocols.FirstOrDefault() ?? string.Empty;

                _webSocket = new WebSocket4Net.WebSocket(uri, subProtocol, sslProtocols: sslProtocols);
                _webSocket.DataReceived += OnDataReceived;
                _webSocket.Open();
                SpinWait.SpinUntil(() => _webSocket.State == WebSocketState.Open, _clientOptions.CommunicationTimeout);

                return Task.FromResult(0);
            }

            public Task DisconnectAsync()
            {
                if (_webSocket != null)
                {
                    _webSocket.DataReceived -= OnDataReceived;
                    _webSocket.Close();
                    SpinWait.SpinUntil(() => _webSocket.State == WebSocketState.Closed, _clientOptions.CommunicationTimeout);
                }

                _webSocket = null;

                return Task.FromResult(0);
            }

            public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                var readBytes = 0;
                while (count > 0 && !cancellationToken.IsCancellationRequested)
                {
                    byte @byte;
                    if (readBytes == 0)
                    {
                        // Block until at lease one byte was received.
                        @byte = _receiveBuffer.Take(cancellationToken);
                    }
                    else
                    {
                        if (!_receiveBuffer.TryTake(out @byte))
                        {
                            return Task.FromResult(readBytes);
                        }
                    }
                    
                    buffer[offset] = @byte;
                    offset++;
                    count--;
                    readBytes++;
                }

                return Task.FromResult(readBytes);
            }

            public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                _webSocket.Send(buffer, offset, count);
                return Task.FromResult(0);
            }

            public void Dispose()
            {
                if (_webSocket != null)
                {
                    _webSocket.DataReceived -= OnDataReceived;
                    _webSocket.Dispose();
                }
            }

            private void OnDataReceived(object sender, WebSocket4Net.DataReceivedEventArgs e)
            {
                foreach (var @byte in e.Data)
                {
                    _receiveBuffer.Add(@byte);
                }
            }
        }
    }
}
