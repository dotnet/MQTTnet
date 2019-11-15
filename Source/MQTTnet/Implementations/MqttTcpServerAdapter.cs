#if !WINDOWS_UWP
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Internal;
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
    public sealed class MqttTcpServerAdapter : Disposable, IMqttServerAdapter
    {
        readonly List<MqttTcpServerListener> _listeners = new List<MqttTcpServerListener>();
        readonly IMqttNetLogger _logger;

        CancellationTokenSource _cancellationTokenSource;

        public MqttTcpServerAdapter(IMqttNetLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            _logger = logger.CreateChildLogger(nameof(MqttTcpServerAdapter));
        }

        public Func<IMqttChannelAdapter, Task> ClientHandler { get; set; }

        public bool TreatSocketOpeningErrorAsWarning { get; set; }

        public Task StartAsync(IMqttServerOptions options)
        {
            if (_cancellationTokenSource != null) throw new InvalidOperationException("Server is already started.");

            _cancellationTokenSource = new CancellationTokenSource();

            if (options.DefaultEndpointOptions.IsEnabled)
            {
                RegisterListeners(options.DefaultEndpointOptions, null, _cancellationTokenSource.Token);
            }

            if (options.TlsEndpointOptions?.IsEnabled == true)
            {
                if (options.TlsEndpointOptions.Certificate == null && options.TlsEndpointOptions.X509Certificate == null)
                {
                    throw new ArgumentException("TLS certificate is not set.");
                }

                X509Certificate2 tlsCertificate;
                if (options.TlsEndpointOptions.X509Certificate != null)
                {
                    tlsCertificate = options.TlsEndpointOptions.X509Certificate;
                }
                else if (string.IsNullOrEmpty(options.TlsEndpointOptions.CertificateCredentials?.Password))
                {
                    // Use a different overload when no password is specified. Otherwise the constructor will fail.
                    tlsCertificate = new X509Certificate2(options.TlsEndpointOptions.Certificate);
                }
                else
                {
                    tlsCertificate = new X509Certificate2(options.TlsEndpointOptions.Certificate, options.TlsEndpointOptions.CertificateCredentials.Password);
                }

                if (!tlsCertificate.HasPrivateKey)
                {
                    throw new InvalidOperationException("The certificate for TLS encryption must contain the private key.");
                }

                RegisterListeners(options.TlsEndpointOptions, tlsCertificate, _cancellationTokenSource.Token);
            }

            return Task.FromResult(0);
        }

        public Task StopAsync()
        {
            Cleanup();
            return Task.FromResult(0);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Cleanup();
            }

            base.Dispose(disposing);
        }

        void Cleanup()
        {
            _cancellationTokenSource?.Cancel(false);
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            foreach (var listener in _listeners)
            {
                listener.Dispose();
            }

            _listeners.Clear();
        }

        void RegisterListeners(MqttServerTcpEndpointBaseOptions options, X509Certificate2 tlsCertificate, CancellationToken cancellationToken)
        {
            if (!options.BoundInterNetworkAddress.Equals(IPAddress.None))
            {
                var listenerV4 = new MqttTcpServerListener(
                    AddressFamily.InterNetwork,
                    options,
                    tlsCertificate,
                    _logger)
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
                var listenerV6 = new MqttTcpServerListener(
                    AddressFamily.InterNetworkV6,
                    options,
                    tlsCertificate,
                    _logger)
                {
                    ClientHandler = OnClientAcceptedAsync
                };

                if (listenerV6.Start(TreatSocketOpeningErrorAsWarning, cancellationToken))
                {
                    _listeners.Add(listenerV6);
                }
            }
        }

        private Task OnClientAcceptedAsync(IMqttChannelAdapter channelAdapter)
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