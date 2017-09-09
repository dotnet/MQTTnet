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
        }

        public IList<ConnectedMqttClient> GetConnectedClients()
        {
            return _clientSessionsManager.GetConnectedClients();
        }

        public event EventHandler<MqttClientConnectedEventArgs> ClientConnected;

        public event EventHandler<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;

        public void Publish(MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            _clientSessionsManager.DispatchPublishPacket(null, applicationMessage.ToPublishPacket());
        }

        public void InjectClient(string identifier, IMqttCommunicationAdapter adapter)
        {
            if (adapter == null) throw new ArgumentNullException(nameof(adapter));

            if (_cancellationTokenSource == null) throw new InvalidOperationException("The MQTT server is not started.");

            OnClientConnected(this, new MqttClientConnectedEventArgs(identifier, adapter));
        }

        public void Start()
        {
            if (_cancellationTokenSource != null) throw new InvalidOperationException("The MQTT server is already started.");

            _cancellationTokenSource = new CancellationTokenSource();

            foreach (var adapter in _adapters)
            {
                adapter.ClientConnected += OnClientConnected;
                adapter.Start(_options);
            }
            
            MqttTrace.Information(nameof(MqttServer), "Started.");
        }

        public void Stop()
        {
            _cancellationTokenSource?.Cancel(false);
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            foreach (var adapter in _adapters)
            {
                adapter.ClientConnected -= OnClientConnected;
                adapter.Stop();
            }

            _clientSessionsManager.Clear();

            MqttTrace.Information(nameof(MqttServer), "Stopped.");
        }

        private void OnClientConnected(object sender, MqttClientConnectedEventArgs eventArgs)
        {
            MqttTrace.Information(nameof(MqttServer), "Client '{0}': Connected.", eventArgs.Identifier);
            ClientConnected?.Invoke(this, eventArgs);

            Task.Run(() => _clientSessionsManager.RunClientSessionAsync(eventArgs), _cancellationTokenSource.Token);
        }
    }
}
