// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Internal;
using MQTTnet.Server;

namespace MQTTnet.Implementations
{
    public sealed class MqttTcpServerAdapter : IMqttServerAdapter
    {
        readonly List<MqttTcpServerListener> _listeners = new List<MqttTcpServerListener>();

        CancellationTokenSource _cancellationTokenSource;

        MqttServerOptions _serverOptions;

        public Func<IMqttChannelAdapter, Task> ClientHandler { get; set; }

        public bool TreatSocketOpeningErrorAsWarning { get; set; }

        public void Dispose()
        {
            Cleanup();
        }

        public Task StartAsync(MqttServerOptions options, IMqttNetLogger logger)
        {
            if (_cancellationTokenSource != null)
            {
                throw new InvalidOperationException("Server is already started.");
            }

            _serverOptions = options;

            _cancellationTokenSource = new CancellationTokenSource();

            if (options.DefaultEndpointOptions.IsEnabled)
            {
                RegisterListeners(options.DefaultEndpointOptions, logger, _cancellationTokenSource.Token);
            }

            if (options.TlsEndpointOptions?.IsEnabled == true)
            {
                if (options.TlsEndpointOptions.CertificateProvider == null)
                {
                    throw new ArgumentException("TLS certificate is not set.");
                }

                RegisterListeners(options.TlsEndpointOptions, logger, _cancellationTokenSource.Token);
            }

            return CompletedTask.Instance;
        }

        public Task StopAsync()
        {
            Cleanup();
            return CompletedTask.Instance;
        }

        void Cleanup()
        {
            try
            {
                _cancellationTokenSource?.Cancel(false);
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                foreach (var listener in _listeners)
                {
                    listener.Dispose();
                }

                _listeners.Clear();
            }
        }

        Task OnClientAcceptedAsync(IMqttChannelAdapter channelAdapter)
        {
            var clientHandler = ClientHandler;
            if (clientHandler == null)
            {
                return CompletedTask.Instance;
            }

            return clientHandler(channelAdapter);
        }

        void RegisterListeners(MqttServerTcpEndpointBaseOptions tcpEndpointOptions, IMqttNetLogger logger, CancellationToken cancellationToken)
        {
            if (!tcpEndpointOptions.BoundInterNetworkAddress.Equals(IPAddress.None))
            {
                var listenerV4 = new MqttTcpServerListener(AddressFamily.InterNetwork, _serverOptions, tcpEndpointOptions, logger)
                {
                    ClientHandler = OnClientAcceptedAsync
                };

                if (listenerV4.Start(TreatSocketOpeningErrorAsWarning, cancellationToken))
                {
                    _listeners.Add(listenerV4);
                }
            }

            if (!tcpEndpointOptions.BoundInterNetworkV6Address.Equals(IPAddress.None))
            {
                var listenerV6 = new MqttTcpServerListener(AddressFamily.InterNetworkV6, _serverOptions, tcpEndpointOptions, logger)
                {
                    ClientHandler = OnClientAcceptedAsync
                };

                if (listenerV6.Start(TreatSocketOpeningErrorAsWarning, cancellationToken))
                {
                    _listeners.Add(listenerV6);
                }
            }
        }
    }
}
