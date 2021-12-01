#if !WINDOWS_UWP
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Implementations
{
    public sealed class MqttTcpServerAdapter : IMqttServerAdapter
    {
        readonly List<MqttTcpServerListener> _listeners = new List<MqttTcpServerListener>();

        CancellationTokenSource _cancellationTokenSource;
        
        public Func<IMqttChannelAdapter, Task> ClientHandler { get; set; }

        public bool TreatSocketOpeningErrorAsWarning { get; set; }

        public Task StartAsync(MqttServerOptions options, IMqttNetLogger logger)
        {
            if (_cancellationTokenSource != null) throw new InvalidOperationException("Server is already started.");

            _cancellationTokenSource = new CancellationTokenSource();

            if (options.DefaultEndpointOptions.IsEnabled)
            {
                RegisterListeners(options.DefaultEndpointOptions, null, logger, _cancellationTokenSource.Token);
            }

            if (options.TlsEndpointOptions?.IsEnabled == true)
            {
                if (options.TlsEndpointOptions.CertificateProvider == null)
                {
                    throw new ArgumentException("TLS certificate is not set.");
                }

                var tlsCertificate = options.TlsEndpointOptions.CertificateProvider.GetCertificate();
                if (!tlsCertificate.HasPrivateKey)
                {
                    throw new InvalidOperationException("The certificate for TLS encryption must contain the private key.");
                }

                RegisterListeners(options.TlsEndpointOptions, tlsCertificate, logger, _cancellationTokenSource.Token);
            }

            return PlatformAbstractionLayer.CompletedTask;
        }

        public Task StopAsync()
        {
            Cleanup();
            return PlatformAbstractionLayer.CompletedTask;
        }

        public void Dispose()
        {
            Cleanup();
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

        void RegisterListeners(MqttServerTcpEndpointBaseOptions options, X509Certificate2 tlsCertificate, IMqttNetLogger logger, CancellationToken cancellationToken)
        {
            if (!options.BoundInterNetworkAddress.Equals(IPAddress.None))
            {
                var listenerV4 = new MqttTcpServerListener(AddressFamily.InterNetwork, options, tlsCertificate, logger)
                {
                    ClientHandler = OnClientAcceptedAsync
                };

                if (listenerV4.Start(TreatSocketOpeningErrorAsWarning, cancellationToken))
                {
                    _listeners.Add(listenerV4);
                }
            }

            if (!options.BoundInterNetworkV6Address.Equals(IPAddress.None))
            {
                var listenerV6 = new MqttTcpServerListener(AddressFamily.InterNetworkV6, options, tlsCertificate, logger)
                {
                    ClientHandler = OnClientAcceptedAsync
                };

                if (listenerV6.Start(TreatSocketOpeningErrorAsWarning, cancellationToken))
                {
                    _listeners.Add(listenerV6);
                }
            }
        }

        Task OnClientAcceptedAsync(IMqttChannelAdapter channelAdapter)
        {
            var clientHandler = ClientHandler;
            if (clientHandler == null)
            {
                return Task.FromResult(0);
            }

            return clientHandler(channelAdapter);
        }
    }
}
#endif