using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Internal;

namespace MQTTnet.Server
{
    public class MqttServer : IMqttServer
    {
        private readonly MqttServerEventDispatcher _eventDispatcher = new MqttServerEventDispatcher();
        private readonly ICollection<IMqttServerAdapter> _adapters;
        private readonly IMqttNetChildLogger _logger;

        private MqttClientSessionsManager _clientSessionsManager;
        private MqttRetainedMessagesManager _retainedMessagesManager;
        private CancellationTokenSource _cancellationTokenSource;

        public MqttServer(IEnumerable<IMqttServerAdapter> adapters, IMqttNetChildLogger logger)
        {
            if (adapters == null) throw new ArgumentNullException(nameof(adapters));
            _adapters = adapters.ToList();

            if (logger == null) throw new ArgumentNullException(nameof(logger));
            _logger = logger.CreateChildLogger(nameof(MqttServer));

            _eventDispatcher.ClientConnected += (s, e) => ClientConnected?.Invoke(s, e);
            _eventDispatcher.ClientDisconnected += (s, e) => ClientDisconnected?.Invoke(s, e);
            _eventDispatcher.ClientSubscribedTopic += (s, e) => ClientSubscribedTopic?.Invoke(s, e);
            _eventDispatcher.ClientUnsubscribedTopic += (s, e) => ClientUnsubscribedTopic?.Invoke(s, e);
            _eventDispatcher.ApplicationMessageReceived += (s, e) => ApplicationMessageReceived?.Invoke(s, e);
        }

        public event EventHandler Started;
        public event EventHandler Stopped;

        public event EventHandler<MqttClientConnectedEventArgs> ClientConnected;
        public event EventHandler<MqttClientDisconnectedEventArgs> ClientDisconnected;
        public event EventHandler<MqttClientSubscribedTopicEventArgs> ClientSubscribedTopic;
        public event EventHandler<MqttClientUnsubscribedTopicEventArgs> ClientUnsubscribedTopic;

        public event EventHandler<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;

        public IMqttServerOptions Options { get; private set; }

        public Task<IList<IMqttClientSessionStatus>> GetClientSessionsStatusAsync()
        {
            return Task.FromResult(_clientSessionsManager.GetClientStatus());
        }

        public IList<IMqttClientSessionStatus> GetClientSessionsStatus()
        {
            return _clientSessionsManager.GetClientStatus();
        }

        public IList<MqttApplicationMessage> GetRetainedMessages()
        {
            return _retainedMessagesManager.GetMessages();
        }

        public Task SubscribeAsync(string clientId, IList<TopicFilter> topicFilters)
        {
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            return _clientSessionsManager.SubscribeAsync(clientId, topicFilters);
        }

        public Task UnsubscribeAsync(string clientId, IList<string> topicFilters)
        {
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            return _clientSessionsManager.UnsubscribeAsync(clientId, topicFilters);
        }

        public Task PublishAsync(MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            if (_cancellationTokenSource == null) throw new InvalidOperationException("The server is not started.");

            _clientSessionsManager.EnqueueApplicationMessage(null, applicationMessage.ToPublishPacket());

            return Task.FromResult(0);
        }

        public async Task StartAsync(IMqttServerOptions options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));

            if (_cancellationTokenSource != null) throw new InvalidOperationException("The server is already started.");

            _cancellationTokenSource = new CancellationTokenSource();

            _retainedMessagesManager = new MqttRetainedMessagesManager(Options, _logger);
            await _retainedMessagesManager.LoadMessagesAsync().ConfigureAwait(false);

            _clientSessionsManager = new MqttClientSessionsManager(Options, _retainedMessagesManager, _cancellationTokenSource.Token, _eventDispatcher, _logger);
            _clientSessionsManager.Start();

            foreach (var adapter in _adapters)
            {
                adapter.ClientAccepted += OnClientAccepted;
                await adapter.StartAsync(Options).ConfigureAwait(false);
            }

            _logger.Info("Started.");
            Started?.Invoke(this, EventArgs.Empty);
        }

        public async Task StopAsync()
        {
            try
            {
                if (_cancellationTokenSource == null)
                {
                    return;
                }
                
                _clientSessionsManager.Stop();
                
                _cancellationTokenSource.Cancel(false);

                foreach (var adapter in _adapters)
                {
                    adapter.ClientAccepted -= OnClientAccepted;
                    await adapter.StopAsync().ConfigureAwait(false);
                }

                _logger.Info("Stopped.");
                Stopped?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                _retainedMessagesManager = null;

                _clientSessionsManager?.Dispose();
                _clientSessionsManager = null;
            }
        }

        public Task ClearRetainedMessagesAsync()
        {
            return _retainedMessagesManager?.ClearMessagesAsync();
        }

        private void OnClientAccepted(object sender, MqttServerAdapterClientAcceptedEventArgs eventArgs)
        {
            eventArgs.SessionTask = _clientSessionsManager.StartSession(eventArgs.Client);
        }
    }
}
