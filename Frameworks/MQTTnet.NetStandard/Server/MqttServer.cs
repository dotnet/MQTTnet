using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;

namespace MQTTnet.Server
{
    public class MqttServer : IMqttServer
    {
        private readonly ICollection<IMqttServerAdapter> _adapters;
        private readonly IMqttNetLogger _logger;

        private MqttClientSessionsManager _clientSessionsManager;
        private MqttRetainedMessagesManager _retainedMessagesManager;
        private CancellationTokenSource _cancellationTokenSource;
        private MqttServerOptions _options;

        public MqttServer(IEnumerable<IMqttServerAdapter> adapters, IMqttNetLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (adapters == null)
            {
                throw new ArgumentNullException(nameof(adapters));
            }

            _adapters = adapters.ToList();
        }

        public event EventHandler<MqttServerStartedEventArgs> Started;
        public event EventHandler<MqttClientConnectedEventArgs> ClientConnected;
        public event EventHandler<MqttClientDisconnectedEventArgs> ClientDisconnected;
        public event EventHandler<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;

        public Task<IList<ConnectedMqttClient>> GetConnectedClientsAsync()
        {
            return _clientSessionsManager.GetConnectedClientsAsync();
        }

        public async Task PublishAsync(IEnumerable<MqttApplicationMessage> applicationMessages)
        {
            if (applicationMessages == null) throw new ArgumentNullException(nameof(applicationMessages));

            if (_cancellationTokenSource == null) throw new InvalidOperationException("The server is not started.");

            foreach (var applicationMessage in applicationMessages)
            {
                await _clientSessionsManager.DispatchApplicationMessageAsync(null, applicationMessage);
            }
        }

        public async Task StartAsync(MqttServerOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            if (_cancellationTokenSource != null) throw new InvalidOperationException("The server is already started.");

            _cancellationTokenSource = new CancellationTokenSource();
            _retainedMessagesManager = new MqttRetainedMessagesManager(_options, _logger);

            _clientSessionsManager = new MqttClientSessionsManager(_options, _retainedMessagesManager, _logger);
            _clientSessionsManager.ApplicationMessageReceived += OnApplicationMessageReceived;
            _clientSessionsManager.ClientConnected += OnClientConnected;
            _clientSessionsManager.ClientDisconnected += OnClientDisconnected;

            await _retainedMessagesManager.LoadMessagesAsync();

            foreach (var adapter in _adapters)
            {
                adapter.ClientAccepted += OnClientAccepted;
                await adapter.StartAsync(_options);
            }

            _logger.Info<MqttServer>("Started.");

            Started?.Invoke(this, new MqttServerStartedEventArgs());
        }

        public async Task StopAsync()
        {
            try
            {
                if (_cancellationTokenSource == null)
                {
                    return;
                }

                _cancellationTokenSource.Cancel(false);
                _cancellationTokenSource.Dispose();

                foreach (var adapter in _adapters)
                {
                    adapter.ClientAccepted -= OnClientAccepted;
                    await adapter.StopAsync();
                }

                await _clientSessionsManager.StopAsync();

                _logger.Info<MqttServer>("Stopped.");
            }
            finally
            {
                _cancellationTokenSource = null;

                _retainedMessagesManager = null;

                if (_clientSessionsManager != null)
                {
                    _clientSessionsManager.ApplicationMessageReceived -= OnApplicationMessageReceived;
                    _clientSessionsManager.ClientConnected -= OnClientConnected;
                    _clientSessionsManager.ClientDisconnected -= OnClientDisconnected;
                }

                _clientSessionsManager = null;
            }
        }

        private void OnClientAccepted(object sender, MqttServerAdapterClientAcceptedEventArgs eventArgs)
        {
            eventArgs.SessionTask = Task.Run(async () => await _clientSessionsManager.RunClientSessionAsync(eventArgs.Client, _cancellationTokenSource.Token), _cancellationTokenSource.Token);
        }

        private void OnClientConnected(object sender, MqttClientConnectedEventArgs eventArgs)
        {
            _logger.Info<MqttServer>("Client '{0}': Connected.", eventArgs.Client.ClientId);
            ClientConnected?.Invoke(this, eventArgs);
        }

        private void OnClientDisconnected(object sender, MqttClientDisconnectedEventArgs eventArgs)
        {
            _logger.Info<MqttServer>("Client '{0}': Disconnected.", eventArgs.Client.ClientId);
            ClientDisconnected?.Invoke(this, eventArgs);
        }

        private void OnApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            ApplicationMessageReceived?.Invoke(this, e);
        }
    }
}
