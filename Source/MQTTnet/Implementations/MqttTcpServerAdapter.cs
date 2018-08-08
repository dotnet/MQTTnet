#if !WINDOWS_UWP
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Server;

namespace MQTTnet.Implementations
{
    public class MqttTcpServerAdapter : IMqttServerAdapter
    {
        private readonly List<MqttTcpServerListener> _listeners = new List<MqttTcpServerListener>();
        private readonly IMqttNetChildLogger _logger;

        private CancellationTokenSource _cancellationTokenSource;

        public MqttTcpServerAdapter(IMqttNetChildLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            _logger = logger.CreateChildLogger(nameof(MqttTcpServerAdapter));
        }

        public event EventHandler<MqttServerAdapterClientAcceptedEventArgs> ClientAccepted;

        public Task StartAsync(IMqttServerOptions options)
        {
            if (_cancellationTokenSource != null) throw new InvalidOperationException("Server is already started.");

            _cancellationTokenSource = new CancellationTokenSource();

            if (options.DefaultEndpointOptions.IsEnabled)
            {
                RegisterListeners(options.DefaultEndpointOptions, null);
            }

            if (options.TlsEndpointOptions.IsEnabled)
            {
                if (options.TlsEndpointOptions.Certificate == null)
                {
                    throw new ArgumentException("TLS certificate is not set.");
                }

                var tlsCertificate = new X509Certificate2(options.TlsEndpointOptions.Certificate);
                if (!tlsCertificate.HasPrivateKey)
                {
                    throw new InvalidOperationException("The certificate for TLS encryption must contain the private key.");
                }

                RegisterListeners(options.TlsEndpointOptions, tlsCertificate);
            }

            return Task.FromResult(0);
        }

        public Task StopAsync()
        {
            Dispose();
            return Task.FromResult(0);
        }

        public void Dispose()
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

        private void RegisterListeners(MqttServerTcpEndpointBaseOptions options, X509Certificate2 tlsCertificate)
        {
            if (!options.BoundInterNetworkAddress.Equals(IPAddress.None))
            {
                var listenerV4 = new MqttTcpServerListener(
                    AddressFamily.InterNetwork,
                    options,
                    tlsCertificate,
                    _cancellationTokenSource.Token,
                    _logger);

                listenerV4.ClientAccepted += OnClientAccepted;
                listenerV4.Start();
                _listeners.Add(listenerV4);
            }

            if (!options.BoundInterNetworkV6Address.Equals(IPAddress.None))
            {
                var listenerV6 = new MqttTcpServerListener(
                    AddressFamily.InterNetworkV6,
                    options,
                    tlsCertificate,
                    _cancellationTokenSource.Token,
                    _logger);

                listenerV6.ClientAccepted += OnClientAccepted;
                listenerV6.Start();
                _listeners.Add(listenerV6);
            }
        }

        private void OnClientAccepted(object sender, MqttServerAdapterClientAcceptedEventArgs e)
        {
            ClientAccepted?.Invoke(this, e);
        }
    }
}
#endif