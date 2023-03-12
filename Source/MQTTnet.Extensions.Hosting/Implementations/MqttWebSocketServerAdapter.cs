using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Internal;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Net.Http;
using MQTTnet.Formatter;
using MQTTnet.Extensions.Hosting;
using MQTTnet.Extensions.Hosting.Options;
using MQTTnet.Extensions.Hosting.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace MQTTnet.Implementations
{
    public class MqttWebSocketServerAdapter : IMqttServerAdapter
    {
        readonly List<MqttWebSocketServerListener> _listeners = new List<MqttWebSocketServerListener>();
        readonly IServiceProvider _services;
        readonly MqttServerHostingOptions _hostingOptions;
        MqttServerOptions _serverOptions;
        IMqttNetLogger _logger;

        public MqttWebSocketServerAdapter(IServiceProvider services, MqttServerHostingOptions hostingOptions)
        {
            _services = services;
            _hostingOptions = hostingOptions;
        }

        public Func<IMqttChannelAdapter, Task> ClientHandler { get; set; }

        public Task StartAsync(MqttServerOptions options, IMqttNetLogger logger)
        {
            _serverOptions = options;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (_hostingOptions.DefaultWebSocketEndpointOptions.IsEnabled)
            {
                _listeners.Add(ActivatorUtilities.CreateInstance<MqttWebSocketServerListener>(_services, options, _hostingOptions.DefaultWebSocketEndpointOptions));
            }

            if (_hostingOptions.DefaultTlsWebSocketEndpointOptions.IsEnabled)
            {
                _listeners.Add(ActivatorUtilities.CreateInstance<MqttWebSocketServerListener>(_services, options, _hostingOptions.DefaultTlsWebSocketEndpointOptions));
            }

            foreach (var listener in _listeners)
            {
                listener.Start(CancellationToken.None);
            }

            return CompletedTask.Instance;
        }

        public Task StopAsync()
        {
            foreach (var listener in _listeners)
            {
                listener.Dispose();
            }

            _listeners.Clear();

            return CompletedTask.Instance;
        }

        public void Dispose()
        {
            foreach (var listener in _listeners)
            {
                listener.Dispose();
            }
        }

    }
}
