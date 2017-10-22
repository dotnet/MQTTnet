using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Core.Adapter;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;

namespace MQTTnet.Core.Server
{
    public sealed class MqttServer : IMqttServer
    {
        private readonly ILogger<MqttServer> _logger;
        private readonly MqttClientSessionsManager _clientSessionsManager;
        private readonly ICollection<IMqttServerAdapter> _adapters;
        private readonly MqttServerOptions _options;

        private CancellationTokenSource _cancellationTokenSource;

        public MqttServer(IOptions<MqttServerOptions> options, IEnumerable<IMqttServerAdapter> adapters, ILogger<MqttServer> logger, MqttClientSessionsManager clientSessionsManager)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clientSessionsManager = clientSessionsManager ?? throw new ArgumentNullException(nameof(clientSessionsManager));

            if (adapters == null)
            {
                throw new ArgumentNullException(nameof(adapters));
            }            
            _adapters = adapters.ToList();

            _clientSessionsManager.ApplicationMessageReceived += (s, e) => ApplicationMessageReceived?.Invoke(s, e);
            _clientSessionsManager.ClientConnected += OnClientConnected;
            _clientSessionsManager.ClientDisconnected += OnClientDisconnected;
        }

        public IList<ConnectedMqttClient> GetConnectedClients()
        {
            return _clientSessionsManager.GetConnectedClients();
        }

        public event EventHandler<MqttClientConnectedEventArgs> ClientConnected;
        public event EventHandler<MqttClientDisconnectedEventArgs> ClientDisconnected;
        public event EventHandler<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;

        public void Publish(MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            _options.ApplicationMessageInterceptor?.Invoke(applicationMessage);
            _clientSessionsManager.DispatchApplicationMessage(null, applicationMessage);
        }

        public async Task StartAsync()
        {
            if (_cancellationTokenSource != null) throw new InvalidOperationException("The MQTT server is already started.");

            _cancellationTokenSource = new CancellationTokenSource();

            await _clientSessionsManager.RetainedMessagesManager.LoadMessagesAsync();

            foreach (var adapter in _adapters)
            {
                adapter.ClientAccepted += OnClientAccepted;
                await adapter.StartAsync(_options);
            }

            _logger.LogInformation("Started.");
        }

        public async Task StopAsync()
        {
            _cancellationTokenSource?.Cancel(false);
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            foreach (var adapter in _adapters)
            {
                adapter.ClientAccepted -= OnClientAccepted;
                await adapter.StopAsync();
            }

            _clientSessionsManager.Clear();

            _logger.LogInformation("Stopped.");
        }

        private void OnClientAccepted(object sender, MqttServerAdapterClientAcceptedEventArgs eventArgs)
        {
            eventArgs.SessionTask = Task.Run(async () => await _clientSessionsManager.RunClientSessionAsync(eventArgs.Client), _cancellationTokenSource.Token);
        }

        private void OnClientConnected(object sender, MqttClientConnectedEventArgs eventArgs)
        {
            _logger.LogInformation("Client '{0}': Connected.", eventArgs.Client.ClientId);
            ClientConnected?.Invoke(this, eventArgs);
        }

        private void OnClientDisconnected(object sender, MqttClientDisconnectedEventArgs eventArgs)
        {
            _logger.LogInformation("Client '{0}': Disconnected.", eventArgs.Client.ClientId);
            ClientDisconnected?.Invoke(this, eventArgs);
        }
    }
}
