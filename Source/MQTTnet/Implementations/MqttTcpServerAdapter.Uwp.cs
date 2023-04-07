// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if WINDOWS_UWP
using Windows.Networking.Sockets;
using MQTTnet.Adapter;
using MQTTnet.Formatter;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using MQTTnet.Server;
using MQTTnet.Diagnostics;

namespace MQTTnet.Implementations
{
    public sealed class MqttTcpServerAdapter : IMqttServerAdapter
    {
        IMqttNetLogger _rootLogger;
        MqttNetSourceLogger _logger;
 
        MqttServerOptions _options;
        StreamSocketListener _listener;

        public Func<IMqttChannelAdapter, Task> ClientHandler { get; set; }

        public async Task StartAsync(MqttServerOptions options, IMqttNetLogger logger)
        {
            if (_listener != null) throw new InvalidOperationException("Server is already started.");

            if (logger is null) throw new ArgumentNullException(nameof(logger));
            _rootLogger = logger;
            _logger = logger.WithSource(nameof(MqttTcpServerAdapter));

            _options = options ?? throw new ArgumentNullException(nameof(options));

            if (options.DefaultEndpointOptions.IsEnabled)
            {
                _listener = new StreamSocketListener();

                // This also affects the client sockets.
                _listener.Control.NoDelay = options.DefaultEndpointOptions.NoDelay;
                _listener.Control.KeepAlive = true;
                _listener.Control.QualityOfService = SocketQualityOfService.LowLatency;
                _listener.ConnectionReceived += OnConnectionReceivedAsync;

                await _listener.BindServiceNameAsync(options.DefaultEndpointOptions.Port.ToString(), SocketProtectionLevel.PlainSocket);
            }

            if (options.TlsEndpointOptions.IsEnabled)
            {
                throw new NotSupportedException("TLS servers are not supported when using 'uap10.0'.");
            }
        }

        public Task StopAsync()
        {
            if (_listener != null)
            {
                _listener.ConnectionReceived -= OnConnectionReceivedAsync;
            }

            return Task.FromResult(0);
        }

        public void Dispose()
        {
            _listener?.Dispose();
            _listener = null;
        }

        async void OnConnectionReceivedAsync(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            try
            {
                var clientHandler = ClientHandler;
                if (clientHandler != null)
                {
                    X509Certificate2 clientCertificate = null;

                    if (args.Socket.Control.ClientCertificate != null)
                    {
                        try
                        {
                            clientCertificate = new X509Certificate2(args.Socket.Control.ClientCertificate.GetCertificateBlob().ToArray());
                        }
                        catch (Exception exception)
                        {
                            _logger.Warning(exception, "Unable to convert UWP certificate to X509Certificate2.");
                        }
                    }

                    var bufferWriter = new MqttBufferWriter(4096, 65535);
                    var packetFormatterAdapter = new MqttPacketFormatterAdapter(bufferWriter);
                    var tcpChannel = new MqttTcpChannel(args.Socket, clientCertificate, _options);

                    using (var clientAdapter = new MqttChannelAdapter(tcpChannel, packetFormatterAdapter, _rootLogger))
                    {
                        await clientHandler(clientAdapter).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception exception)
            {
                if (exception is ObjectDisposedException)
                {
                    // It can happen that the listener socket is accessed after the cancellation token is already set and the listener socket is disposed.
                    return;
                }

                _logger.Error(exception, "Error while handling client connection.");
            }
            finally
            {
                try
                {
                    args.Socket.Dispose();
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, "Error while cleaning up client connection.");
                }
            }
        }
    }
}
#endif