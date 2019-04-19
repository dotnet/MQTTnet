using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Receiving;
using MQTTnet.Diagnostics;
using MQTTnet.Protocol;
using MQTTnet.Server.Status;

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
        }

        public IMqttServerStartedHandler StartedHandler { get; set; }

        public IMqttServerStoppedHandler StoppedHandler { get; set; }

        public IMqttServerClientConnectedHandler ClientConnectedHandler
        {
            get => _eventDispatcher.ClientConnectedHandler;
            set => _eventDispatcher.ClientConnectedHandler = value;
        }

        public IMqttServerClientDisconnectedHandler ClientDisconnectedHandler
        {
            get => _eventDispatcher.ClientDisconnectedHandler;
            set => _eventDispatcher.ClientDisconnectedHandler = value;
        }
        
        public IMqttServerClientSubscribedTopicHandler ClientSubscribedTopicHandler
        {
            get => _eventDispatcher.ClientSubscribedTopicHandler;
            set => _eventDispatcher.ClientSubscribedTopicHandler = value;
        }

        public IMqttServerClientUnsubscribedTopicHandler ClientUnsubscribedTopicHandler
        {
            get => _eventDispatcher.ClientUnsubscribedTopicHandler;
            set => _eventDispatcher.ClientUnsubscribedTopicHandler = value;
        }
        
        public IMqttApplicationMessageReceivedHandler ApplicationMessageReceivedHandler
        {
            get => _eventDispatcher.ApplicationMessageReceivedHandler;
            set => _eventDispatcher.ApplicationMessageReceivedHandler = value;
        }

        public IMqttServerOptions Options { get; private set; }

        public Task<IList<IMqttClientStatus>> GetClientStatusAsync()
        {
            return _clientSessionsManager.GetClientStatusAsync();
        }

        public Task<IList<IMqttSessionStatus>> GetSessionStatusAsync()
        {
            return _clientSessionsManager.GetSessionStatusAsync();
        }

        public Task<IList<MqttApplicationMessage>> GetRetainedApplicationMessagesAsync()
        {
            return _retainedMessagesManager.GetMessagesAsync();
        }

        public Task SubscribeAsync(string clientId, ICollection<TopicFilter> topicFilters)
        {
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            return _clientSessionsManager.SubscribeAsync(clientId, topicFilters);
        }

        public Task UnsubscribeAsync(string clientId, ICollection<string> topicFilters)
        {
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            return _clientSessionsManager.UnsubscribeAsync(clientId, topicFilters);
        }

        public ValueTask<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            MqttTopicValidator.ThrowIfInvalid(applicationMessage.Topic);

            if (_cancellationTokenSource == null) throw new InvalidOperationException("The server is not started.");

            _clientSessionsManager.DispatchApplicationMessage(applicationMessage, null);

            return new ValueTask<MqttClientPublishResult>(new MqttClientPublishResult());
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
                adapter.ClientHandler = OnHandleClient;
                await adapter.StartAsync(Options).ConfigureAwait(false);
            }

            _logger.Info("Started.");

            var startedHandler = StartedHandler;
            if (startedHandler != null)
            {
                await startedHandler.HandleServerStartedAsync(EventArgs.Empty).ConfigureAwait(false);
            }
        }

        public async Task StopAsync()
        {
            try
            {
                if (_cancellationTokenSource == null)
                {
                    return;
                }
                
                await _clientSessionsManager.StopAsync().ConfigureAwait(false);
                
                _cancellationTokenSource.Cancel(false);

                foreach (var adapter in _adapters)
                {
                    adapter.ClientHandler = null;
                    await adapter.StopAsync().ConfigureAwait(false);
                }

                _logger.Info("Stopped.");
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                _retainedMessagesManager = null;

                _clientSessionsManager?.Dispose();
                _clientSessionsManager = null;
            }

            var stoppedHandler = StoppedHandler;
            if (stoppedHandler != null)
            {
                await stoppedHandler.HandleServerStoppedAsync(EventArgs.Empty).ConfigureAwait(false);
            }
        }

        public Task ClearRetainedApplicationMessagesAsync()
        {
            return _retainedMessagesManager?.ClearMessagesAsync();
        }

        private Task OnHandleClient(IMqttChannelAdapter channelAdapter)
        {
            return _clientSessionsManager.HandleClientAsync(channelAdapter);
        }
    }
}
