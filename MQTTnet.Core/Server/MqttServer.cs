using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Internal;

namespace MQTTnet.Core.Server
{
    public sealed class MqttServer : IMqttServer
    {
        private readonly MqttClientSessionsManager _clientSessionsManager;
        private readonly ICollection<IMqttServerAdapter> _adapters;
        private readonly MqttServerOptions _options;

        private CancellationTokenSource _cancellationTokenSource;

        public MqttServer(MqttServerOptions options, ICollection<IMqttServerAdapter> adapters)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _adapters = adapters ?? throw new ArgumentNullException(nameof(adapters));

            _clientSessionsManager = new MqttClientSessionsManager(options);
            _clientSessionsManager.ApplicationMessageReceived += (s, e) => ApplicationMessageReceived?.Invoke(s, e);
            _clientSessionsManager.ClientConnected += OnClientConnected;
            _clientSessionsManager.ClientDisconnected += OnClientDisconnected;
        }

        public IReadOnlyList<ConnectedMqttClient> GetConnectedClients()
        {
            return _clientSessionsManager.GetConnectedClients();
        }

        public event EventHandler<MqttClientConnectedEventArgs> ClientConnected;
        public event EventHandler<MqttClientDisconnectedEventArgs> ClientDisconnected;
        public event EventHandler<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;

        public void Publish(MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            _clientSessionsManager.DispatchPublishPacket(null, applicationMessage.ToPublishPacket());
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

            MqttNetTrace.Information(nameof(MqttServer), "Started.");
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

            MqttNetTrace.Information(nameof(MqttServer), "Stopped.");
        }

        private void OnClientAccepted(object sender, MqttServerAdapterClientAcceptedEventArgs eventArgs)
        {
            Task.Run(() =>_clientSessionsManager.RunClientSessionAsync(eventArgs.Client), _cancellationTokenSource.Token);
        }

        private void OnClientConnected(object sender, MqttClientConnectedEventArgs eventArgs)
        {
            MqttNetTrace.Information(nameof(MqttServer), "Client '{0}': Connected.", eventArgs.Client.ClientId);
            ClientConnected?.Invoke(this, eventArgs);
        }

        private void OnClientDisconnected(object sender, MqttClientDisconnectedEventArgs eventArgs)
        {
            MqttNetTrace.Information(nameof(MqttServer), "Client '{0}': Disconnected.", eventArgs.Client.ClientId);
            ClientDisconnected?.Invoke(this, eventArgs);
        }
    }
}
