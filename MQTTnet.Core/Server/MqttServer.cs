using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Internal;

namespace MQTTnet.Core.Server
{
    public class MqttServer
    {
        private readonly MqttClientSessionManager _clientSessionManager;
        private readonly IMqttServerAdapter _adapter;
        private readonly MqttServerOptions _options;

        private CancellationTokenSource _cancellationTokenSource;

        public MqttServer(MqttServerOptions options, IMqttServerAdapter adapter)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            
            _clientSessionManager = new MqttClientSessionManager(options);
        }

        public void InjectClient(string identifier, IMqttCommunicationAdapter adapter)
        {
            if (adapter == null) throw new ArgumentNullException(nameof(adapter));

            OnClientConnected(this, new MqttClientConnectedEventArgs(identifier, adapter));
        }

        public void Start()
        {
            if (_cancellationTokenSource != null)
            {
                throw new InvalidOperationException("The server is already started.");
            }

            _cancellationTokenSource = new CancellationTokenSource();

            _adapter.ClientConnected += OnClientConnected;
            _adapter.Start(_options);

            MqttTrace.Information(nameof(MqttServer), "Started.");
        }

        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;

            _adapter.ClientConnected -= OnClientConnected;
            _adapter.Stop();

            _clientSessionManager.Clear();

            MqttTrace.Information(nameof(MqttServer), "Stopped.");
        }

        private void OnClientConnected(object sender, MqttClientConnectedEventArgs eventArgs)
        {
            MqttTrace.Information(nameof(MqttServer), $"Client '{eventArgs.Identifier}': Connected.");
            Task.Run(async () => await _clientSessionManager.RunClientSessionAsync(eventArgs), _cancellationTokenSource.Token).Forget();
        }
    }
}
