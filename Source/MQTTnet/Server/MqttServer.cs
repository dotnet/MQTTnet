using MQTTnet.Adapter;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Receiving;
using MQTTnet.Exceptions;
using MQTTnet.Protocol;
using MQTTnet.Server.Status;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Implementations;
using MQTTnet.Internal;
using MQTTnet.Server.Internal;

namespace MQTTnet.Server
{
    public sealed class MqttServer : Disposable, IMqttServer
    {
        readonly AsyncEvent<EventArgs> _startedEvent = new AsyncEvent<EventArgs>();
        readonly AsyncEvent<EventArgs> _stoppedEvent = new AsyncEvent<EventArgs>();
        
        readonly MqttServerEventDispatcher _eventDispatcher;
        readonly ICollection<IMqttServerAdapter> _adapters;
        readonly IMqttNetLogger _rootLogger;
        readonly MqttNetSourceLogger _logger;

        MqttClientSessionsManager _clientSessionsManager;
        IMqttRetainedMessagesManager _retainedMessagesManager;
        MqttServerKeepAliveMonitor _keepAliveMonitor;
        CancellationTokenSource _cancellationTokenSource;

        public MqttServer(IEnumerable<IMqttServerAdapter> adapters, IMqttNetLogger logger)
        {
            if (adapters == null) throw new ArgumentNullException(nameof(adapters));
            _adapters = adapters.ToList();

            if (logger == null) throw new ArgumentNullException(nameof(logger));
            _logger = logger.WithSource(nameof(MqttServer));
            _rootLogger = logger;

            _eventDispatcher = new MqttServerEventDispatcher(logger);
        }

        public bool IsStarted => _cancellationTokenSource != null;

        public IMqttServerStartedHandler StartedHandler { get; set; }

        public event Func<EventArgs, Task> StartedAsync
        {
            add => _startedEvent.AddHandler(value);
            remove => _startedEvent.RemoveHandler(value);
        }

        public IMqttServerStoppedHandler StoppedHandler { get; set; }

        public event Func<EventArgs, Task> StoppedAsync
        {
            add => _stoppedEvent.AddHandler(value);
            remove => _stoppedEvent.RemoveHandler(value);
        }
        
        public IMqttServerClientConnectedHandler ClientConnectedHandler
        {
            get => _eventDispatcher.ClientConnectedHandler;
            set => _eventDispatcher.ClientConnectedHandler = value;
        }

        public event Func<MqttServerClientConnectedEventArgs, Task> ClientConnectedAsync
        {
            add => _eventDispatcher.ClientConnectedEvent.AddHandler(value);
            remove => _eventDispatcher.ClientConnectedEvent.RemoveHandler(value);
        }

        public IMqttServerClientDisconnectedHandler ClientDisconnectedHandler
        {
            get => _eventDispatcher.ClientDisconnectedHandler;
            set => _eventDispatcher.ClientDisconnectedHandler = value;
        }

        public event Func<MqttServerClientDisconnectedEventArgs, Task> ClientDisconnectedAsync
        {
            add => _eventDispatcher.ClientDisconnectedEvent.AddHandler(value);
            remove => _eventDispatcher.ClientDisconnectedEvent.RemoveHandler(value);
        }
        
        public IMqttServerClientSubscribedTopicHandler ClientSubscribedTopicHandler
        {
            get => _eventDispatcher.ClientSubscribedTopicHandler;
            set => _eventDispatcher.ClientSubscribedTopicHandler = value;
        }

        public event Func<MqttServerClientSubscribedTopicEventArgs, Task> ClientSubscribedTopicAsync
        {
            add => _eventDispatcher.ClientSubscribedTopicEvent.AddHandler(value);
            remove => _eventDispatcher.ClientSubscribedTopicEvent.RemoveHandler(value);
        }
        
        public IMqttServerClientUnsubscribedTopicHandler ClientUnsubscribedTopicHandler
        {
            get => _eventDispatcher.ClientUnsubscribedTopicHandler;
            set => _eventDispatcher.ClientUnsubscribedTopicHandler = value;
        }
        
        public event Func<MqttServerClientUnsubscribedTopicEventArgs, Task> ClientUnsubscribedTopicAsync
        {
            add => _eventDispatcher.ClientUnsubscribedTopicEvent.AddHandler(value);
            remove => _eventDispatcher.ClientUnsubscribedTopicEvent.RemoveHandler(value);
        }

        public IMqttApplicationMessageReceivedHandler ApplicationMessageReceivedHandler
        {
            get => _eventDispatcher.ApplicationMessageReceivedHandler;
            set => _eventDispatcher.ApplicationMessageReceivedHandler = value;
        }
        
        public event Func<MqttApplicationMessageReceivedEventArgs, Task> ApplicationMessageReceivedAsync
        {
            add => _eventDispatcher.ApplicationMessageReceivedEvent.AddHandler(value);
            remove => _eventDispatcher.ApplicationMessageReceivedEvent.RemoveHandler(value);
        }
        
        public IMqttServerOptions Options { get; private set; }

        public Task<IList<IMqttClientStatus>> GetClientStatusAsync()
        {
            ThrowIfDisposed();
            ThrowIfNotStarted();

            return _clientSessionsManager.GetClientStatusAsync();
        }

        public Task<IList<IMqttSessionStatus>> GetSessionStatusAsync()
        {
            ThrowIfDisposed();
            ThrowIfNotStarted();

            return _clientSessionsManager.GetSessionStatusAsync();
        }

        public Task<IList<MqttApplicationMessage>> GetRetainedApplicationMessagesAsync()
        {
            ThrowIfDisposed();
            ThrowIfNotStarted();

            return _retainedMessagesManager.GetMessagesAsync();
        }

        public Task ClearRetainedApplicationMessagesAsync()
        {
            ThrowIfDisposed();
            ThrowIfNotStarted();

            return _retainedMessagesManager?.ClearMessagesAsync() ?? PlatformAbstractionLayer.CompletedTask;
        }

        public Task SubscribeAsync(string clientId, ICollection<MqttTopicFilter> topicFilters)
        {
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            foreach (var topicFilter in topicFilters)
            {
                MqttTopicValidator.ThrowIfInvalidSubscribe(topicFilter.Topic);
            }

            ThrowIfDisposed();
            ThrowIfNotStarted();

            return _clientSessionsManager.SubscribeAsync(clientId, topicFilters);
        }

        public Task UnsubscribeAsync(string clientId, ICollection<string> topicFilters)
        {
            if (clientId == null) throw new ArgumentNullException(nameof(clientId));
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            ThrowIfDisposed();
            ThrowIfNotStarted();

            return _clientSessionsManager.UnsubscribeAsync(clientId, topicFilters);
        }

        public async Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            ThrowIfDisposed();

            MqttTopicValidator.ThrowIfInvalid(applicationMessage.Topic);

            ThrowIfNotStarted();

            await _clientSessionsManager.DispatchPublishPacket(applicationMessage, null);

            return new MqttClientPublishResult();
        }

        public async Task StartAsync(IMqttServerOptions options)
        {
            ThrowIfDisposed();
            ThrowIfStarted();

            Options = options ?? throw new ArgumentNullException(nameof(options));
            
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            _retainedMessagesManager = Options.RetainedMessagesManager ?? throw new MqttConfigurationException("options.RetainedMessagesManager should not be null.");

            await _retainedMessagesManager.Start(Options, _rootLogger).ConfigureAwait(false);
            await _retainedMessagesManager.LoadMessagesAsync().ConfigureAwait(false);

            _clientSessionsManager = new MqttClientSessionsManager(Options, _retainedMessagesManager, _eventDispatcher, _rootLogger);
            //_clientSessionsManager.Start(cancellationToken);

            _keepAliveMonitor = new MqttServerKeepAliveMonitor(Options, _clientSessionsManager, _rootLogger);
            _keepAliveMonitor.Start(cancellationToken);

            foreach (var adapter in _adapters)
            {
                adapter.ClientHandler = c => OnHandleClient(c, cancellationToken);
                await adapter.StartAsync(Options).ConfigureAwait(false);
            }

            _logger.Info("Started.");

            var startedHandler = StartedHandler;
            if (startedHandler != null)
            {
                await startedHandler.HandleServerStartedAsync(EventArgs.Empty).ConfigureAwait(false);
            }
            
            await _startedEvent.InvokeAsync(EventArgs.Empty).ConfigureAwait(false);
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

                await _clientSessionsManager.CloseAllConnectionsAsync().ConfigureAwait(false);
                
                foreach (var adapter in _adapters)
                {
                    adapter.ClientHandler = null;
                    await adapter.StopAsync().ConfigureAwait(false);
                }

                _logger.Info("Stopped.");
            }
            finally
            {
                //_clientSessionsManager?.Dispose();
                _clientSessionsManager = null;

                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                _retainedMessagesManager = null;
            }

            var stoppedHandler = StoppedHandler;
            if (stoppedHandler != null)
            {
                await stoppedHandler.HandleServerStoppedAsync(EventArgs.Empty).ConfigureAwait(false);
            }

            await _stoppedEvent.InvokeAsync(EventArgs.Empty).ConfigureAwait(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopAsync().GetAwaiter().GetResult();

                foreach (var adapter in _adapters)
                {
                    adapter.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        Task OnHandleClient(IMqttChannelAdapter channelAdapter, CancellationToken cancellationToken)
        {
            return _clientSessionsManager.HandleClientConnectionAsync(channelAdapter, cancellationToken);
        }

        void ThrowIfStarted()
        {
            if (_cancellationTokenSource != null)
            {
                throw new InvalidOperationException("The MQTT server is already started.");
            }
        }

        void ThrowIfNotStarted()
        {
            if (_cancellationTokenSource == null)
            {
                throw new InvalidOperationException("The MQTT server is not started.");
            }
        }
    }
}
