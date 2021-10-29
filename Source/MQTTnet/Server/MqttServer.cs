using MQTTnet.Adapter;
using MQTTnet.Exceptions;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Implementations;
using MQTTnet.Internal;
using MQTTnet.Packets;

namespace MQTTnet.Server
{
    public class MqttServer : Disposable
    {
        readonly MqttServerEventContainer _eventContainer = new MqttServerEventContainer();
        
        readonly ICollection<IMqttServerAdapter> _adapters;
        readonly MqttServerOptions _options;
        readonly IMqttNetLogger _rootLogger;
        readonly MqttNetSourceLogger _logger;

        MqttClientSessionsManager _clientSessionsManager;
        IMqttRetainedMessagesManager _retainedMessagesManager;
        MqttServerKeepAliveMonitor _keepAliveMonitor;
        CancellationTokenSource _cancellationTokenSource;

        public MqttServer(MqttServerOptions options, IEnumerable<IMqttServerAdapter> adapters, IMqttNetLogger logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            
            if (adapters == null) throw new ArgumentNullException(nameof(adapters));
            _adapters = adapters.ToList();

            if (logger == null) throw new ArgumentNullException(nameof(logger));
            _logger = logger.WithSource(nameof(MqttServer));
            _rootLogger = logger;
        }

        public event Func<EventArgs, Task> StartedAsync
        {
            add => _eventContainer.StartedEvent.AddHandler(value);
            remove => _eventContainer.StartedEvent.RemoveHandler(value);
        }
        
        public event Func<EventArgs, Task> StoppedAsync
        {
            add => _eventContainer.StoppedEvent.AddHandler(value);
            remove => _eventContainer.StoppedEvent.RemoveHandler(value);
        }
        
        public event Func<ValidatingMqttClientConnectionEventArgs, Task> ValidatingClientConnectionAsync
        {
            add => _eventContainer.ValidatingClientConnectionEvent.AddHandler(value);
            remove => _eventContainer.ValidatingClientConnectionEvent.RemoveHandler(value);
        }
        
        public event Func<MqttServerClientConnectedEventArgs, Task> ClientConnectedAsync
        {
            add => _eventContainer.ClientConnectedEvent.AddHandler(value);
            remove => _eventContainer.ClientConnectedEvent.RemoveHandler(value);
        }
        
        public event Func<MqttServerClientDisconnectedEventArgs, Task> ClientDisconnectedAsync
        {
            add => _eventContainer.ClientDisconnectedEvent.AddHandler(value);
            remove => _eventContainer.ClientDisconnectedEvent.RemoveHandler(value);
        }
        
        public event Func<InterceptingPacketEventArgs, Task> InterceptingInboundPacketAsync
        {
            add => _eventContainer.InterceptingInboundPacketEvent.AddHandler(value);
            remove => _eventContainer.InterceptingInboundPacketEvent.RemoveHandler(value);
        }
        
        public event Func<InterceptingPacketEventArgs, Task> InterceptingOutboundPacketAsync
        {
            add => _eventContainer.InterceptingOutboundPacketEvent.AddHandler(value);
            remove => _eventContainer.InterceptingOutboundPacketEvent.RemoveHandler(value);
        }
        
        public event Func<InterceptingMqttClientSubscriptionEventArgs, Task> InterceptingClientSubscriptionAsync
        {
            add => _eventContainer.InterceptingClientSubscriptionEvent.AddHandler(value);
            remove => _eventContainer.InterceptingClientSubscriptionEvent.RemoveHandler(value);
        }
        
        public event Func<InterceptingMqttClientUnsubscriptionEventArgs, Task> InterceptingClientUnsubscriptionAsync
        {
            add => _eventContainer.InterceptingClientUnsubscriptionEvent.AddHandler(value);
            remove => _eventContainer.InterceptingClientUnsubscriptionEvent.RemoveHandler(value);
        }
        
        public event Func<InterceptingMqttClientPublishEventArgs, Task> InterceptingClientPublishAsync
        {
            add => _eventContainer.InterceptingClientPublishEvent.AddHandler(value);
            remove => _eventContainer.InterceptingClientPublishEvent.RemoveHandler(value);
        }
        
        public event Func<MqttServerClientSubscribedTopicEventArgs, Task> ClientSubscribedTopicAsync
        {
            add => _eventContainer.ClientSubscribedTopicEvent.AddHandler(value);
            remove => _eventContainer.ClientSubscribedTopicEvent.RemoveHandler(value);
        }
        
        public event Func<MqttServerClientUnsubscribedTopicEventArgs, Task> ClientUnsubscribedTopicAsync
        {
            add => _eventContainer.ClientUnsubscribedTopicEvent.AddHandler(value);
            remove => _eventContainer.ClientUnsubscribedTopicEvent.RemoveHandler(value);
        }
        
        public event Func<PreparingMqttClientSessionEventArgs, Task> PreparingClientSessionAsync
        {
            add => _eventContainer.PreparingClientSessionEvent.AddHandler(value);
            remove => _eventContainer.PreparingClientSessionEvent.RemoveHandler(value);
        }

        public event Func<MqttApplicationMessageNotConsumedEventArgs, Task> ApplicationMessageNotConsumedAsync
        {
            add => _eventContainer.ApplicationMessageNotConsumedEvent.AddHandler(value);
            remove => _eventContainer.ApplicationMessageNotConsumedEvent.RemoveHandler(value);
        }

        public event Func<EventArgs, Task> RetainedApplicationMessageChangedAsync
        {
            add => _eventContainer.RetainedApplicationMessageChangedEvent.AddHandler(value);
            remove => _eventContainer.RetainedApplicationMessageChangedEvent.RemoveHandler(value);
        }
        
        public event Func<EventArgs, Task> RetainedApplicationMessagesClearedAsync
        {
            add => _eventContainer.RetainedApplicationMessageClearedEvent.AddHandler(value);
            remove => _eventContainer.RetainedApplicationMessageClearedEvent.RemoveHandler(value);
        }

        public bool IsStarted => _cancellationTokenSource != null;
        
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

        public async Task<MqttClientPublishResult> PublishAsync(string senderClientId, MqttApplicationMessage applicationMessage, CancellationToken cancellationToken = default)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            ThrowIfDisposed();

            MqttTopicValidator.ThrowIfInvalid(applicationMessage.Topic);

            ThrowIfNotStarted();

            await _clientSessionsManager.DispatchPublishPacket(senderClientId, applicationMessage);

            return new MqttClientPublishResult();
        }
        
        public async Task StartAsync()
        {
            ThrowIfDisposed();
            ThrowIfStarted();
            
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            _retainedMessagesManager = _options.RetainedMessagesManager ?? throw new MqttConfigurationException("options.RetainedMessagesManager should not be null.");

            await _retainedMessagesManager.Start(_options, _rootLogger).ConfigureAwait(false);
            await _retainedMessagesManager.LoadMessagesAsync().ConfigureAwait(false);

            _clientSessionsManager = new MqttClientSessionsManager(_options, _retainedMessagesManager, _eventContainer, _rootLogger);
            
            _keepAliveMonitor = new MqttServerKeepAliveMonitor(_options, _clientSessionsManager, _rootLogger);
            _keepAliveMonitor.Start(cancellationToken);

            foreach (var adapter in _adapters)
            {
                adapter.ClientHandler = c => OnHandleClient(c, cancellationToken);
                await adapter.StartAsync(_options, _rootLogger).ConfigureAwait(false);
            }

            _logger.Info("Started.");

            await _eventContainer.StartedEvent.InvokeAsync(EventArgs.Empty).ConfigureAwait(false);
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

            await _eventContainer.StoppedEvent.InvokeAsync(EventArgs.Empty).ConfigureAwait(false);
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
