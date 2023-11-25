using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Extensions.Hosting.Options;
using MQTTnet.Internal;
using MQTTnet.Server;

namespace MQTTnet.Extensions.Hosting.Implementations
{
    public sealed class MqttWebSocketServerAdapter : IMqttServerAdapter
    {
        readonly MqttServerHostingOptions _hostingOptions;
        readonly List<MqttWebSocketServerListener> _listeners = new List<MqttWebSocketServerListener>();
        readonly IServiceProvider _services;

        public MqttWebSocketServerAdapter(IServiceProvider services, MqttServerHostingOptions hostingOptions)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _hostingOptions = hostingOptions ?? throw new ArgumentNullException(nameof(hostingOptions));
        }

        public Func<IMqttChannelAdapter, Task>? ClientHandler { get; set; }

        public void Dispose()
        {
            foreach (var listener in _listeners)
            {
                listener.Dispose();
            }
        }

        public Task StartAsync(MqttServerOptions options, IMqttNetLogger logger)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

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
    }
}