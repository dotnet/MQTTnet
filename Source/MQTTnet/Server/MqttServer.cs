// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Implementations;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public class MqttServer : Disposable
    {
        readonly MqttServerEventContainer _eventContainer = new MqttServerEventContainer();

        readonly IDictionary _sessionItems = new ConcurrentDictionary<object, object>();
        readonly ICollection<IMqttServerAdapter> _adapters;
        readonly MqttClientSessionsManager _clientSessionsManager;
        readonly MqttServerKeepAliveMonitor _keepAliveMonitor;
        readonly MqttNetSourceLogger _logger;
        readonly MqttServerOptions _options;
        readonly MqttRetainedMessagesManager _retainedMessagesManager;
        readonly IMqttNetLogger _rootLogger;

        CancellationTokenSource _cancellationTokenSource;

        public MqttServer(MqttServerOptions options, IEnumerable<IMqttServerAdapter> adapters, IMqttNetLogger logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            if (adapters == null)
            {
                throw new ArgumentNullException(nameof(adapters));
            }

            _adapters = adapters.ToList();

            _rootLogger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger = logger.WithSource(nameof(MqttServer));

            _retainedMessagesManager = new MqttRetainedMessagesManager(_eventContainer, _rootLogger);
            _clientSessionsManager = new MqttClientSessionsManager(options, _retainedMessagesManager, _eventContainer, _rootLogger);
            _keepAliveMonitor = new MqttServerKeepAliveMonitor(options, _clientSessionsManager, _rootLogger);
        }

        public event Func<ApplicationMessageNotConsumedEventArgs, Task> ApplicationMessageNotConsumedAsync
        {
            add => _eventContainer.ApplicationMessageNotConsumedEvent.AddHandler(value);
            remove => _eventContainer.ApplicationMessageNotConsumedEvent.RemoveHandler(value);
        }

        public event Func<ClientAcknowledgedPublishPacketEventArgs, Task> ClientAcknowledgedPublishPacketAsync
        {
            add => _eventContainer.ClientAcknowledgedPublishPacketEvent.AddHandler(value);
            remove => _eventContainer.ClientAcknowledgedPublishPacketEvent.RemoveHandler(value);
        }

        public event Func<ClientConnectedEventArgs, Task> ClientConnectedAsync
        {
            add => _eventContainer.ClientConnectedEvent.AddHandler(value);
            remove => _eventContainer.ClientConnectedEvent.RemoveHandler(value);
        }

        public event Func<ClientDisconnectedEventArgs, Task> ClientDisconnectedAsync
        {
            add => _eventContainer.ClientDisconnectedEvent.AddHandler(value);
            remove => _eventContainer.ClientDisconnectedEvent.RemoveHandler(value);
        }

        public event Func<ClientSubscribedTopicEventArgs, Task> ClientSubscribedTopicAsync
        {
            add => _eventContainer.ClientSubscribedTopicEvent.AddHandler(value);
            remove => _eventContainer.ClientSubscribedTopicEvent.RemoveHandler(value);
        }

        public event Func<ClientUnsubscribedTopicEventArgs, Task> ClientUnsubscribedTopicAsync
        {
            add => _eventContainer.ClientUnsubscribedTopicEvent.AddHandler(value);
            remove => _eventContainer.ClientUnsubscribedTopicEvent.RemoveHandler(value);
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

        public event Func<InterceptingPublishEventArgs, Task> InterceptingPublishAsync
        {
            add => _eventContainer.InterceptingPublishEvent.AddHandler(value);
            remove => _eventContainer.InterceptingPublishEvent.RemoveHandler(value);
        }

        public event Func<InterceptingSubscriptionEventArgs, Task> InterceptingSubscriptionAsync
        {
            add => _eventContainer.InterceptingSubscriptionEvent.AddHandler(value);
            remove => _eventContainer.InterceptingSubscriptionEvent.RemoveHandler(value);
        }

        public event Func<InterceptingUnsubscriptionEventArgs, Task> InterceptingUnsubscriptionAsync
        {
            add => _eventContainer.InterceptingUnsubscriptionEvent.AddHandler(value);
            remove => _eventContainer.InterceptingUnsubscriptionEvent.RemoveHandler(value);
        }

        public event Func<LoadingRetainedMessagesEventArgs, Task> LoadingRetainedMessageAsync
        {
            add => _eventContainer.LoadingRetainedMessagesEvent.AddHandler(value);
            remove => _eventContainer.LoadingRetainedMessagesEvent.RemoveHandler(value);
        }

        public event Func<EventArgs, Task> PreparingSessionAsync
        {
            add => _eventContainer.PreparingSessionEvent.AddHandler(value);
            remove => _eventContainer.PreparingSessionEvent.RemoveHandler(value);
        }

        public event Func<RetainedMessageChangedEventArgs, Task> RetainedMessageChangedAsync
        {
            add => _eventContainer.RetainedMessageChangedEvent.AddHandler(value);
            remove => _eventContainer.RetainedMessageChangedEvent.RemoveHandler(value);
        }

        public event Func<EventArgs, Task> RetainedMessagesClearedAsync
        {
            add => _eventContainer.RetainedMessagesClearedEvent.AddHandler(value);
            remove => _eventContainer.RetainedMessagesClearedEvent.RemoveHandler(value);
        }

        public event Func<SessionDeletedEventArgs, Task> SessionDeletedAsync
        {
            add => _eventContainer.SessionDeletedEvent.AddHandler(value);
            remove => _eventContainer.SessionDeletedEvent.RemoveHandler(value);
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

        public event Func<ValidatingConnectionEventArgs, Task> ValidatingConnectionAsync
        {
            add => _eventContainer.ValidatingConnectionEvent.AddHandler(value);
            remove => _eventContainer.ValidatingConnectionEvent.RemoveHandler(value);
        }

        public bool IsStarted => _cancellationTokenSource != null;

        public Task DeleteRetainedMessagesAsync()
        {
            ThrowIfNotStarted();

            return _retainedMessagesManager?.ClearMessages() ?? PlatformAbstractionLayer.CompletedTask;
        }

        public Task DisconnectClientAsync(string id, MqttDisconnectReasonCode reasonCode)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            ThrowIfNotStarted();

            return _clientSessionsManager.GetClient(id).StopAsync(reasonCode);
        }

        public Task<IList<MqttClientStatus>> GetClientsAsync()
        {
            ThrowIfNotStarted();

            return _clientSessionsManager.GetClientStatusesAsync();
        }

        public Task<IList<MqttApplicationMessage>> GetRetainedMessagesAsync()
        {
            ThrowIfNotStarted();

            return _retainedMessagesManager.GetMessages();
        }

        public Task<IList<MqttSessionStatus>> GetSessionsAsync()
        {
            ThrowIfNotStarted();

            return _clientSessionsManager.GetSessionStatusAsync();
        }

        public async Task InjectApplicationMessage(InjectedMqttApplicationMessage injectedApplicationMessage)
        {
            if (injectedApplicationMessage == null)
            {
                throw new ArgumentNullException(nameof(injectedApplicationMessage));
            }

            if (injectedApplicationMessage.ApplicationMessage == null)
            {
                throw new ArgumentNullException(nameof(injectedApplicationMessage.ApplicationMessage));
            }

            MqttTopicValidator.ThrowIfInvalid(injectedApplicationMessage.ApplicationMessage.Topic);

            ThrowIfNotStarted();

            var processPublish = true;
            var applicationMessage = injectedApplicationMessage.ApplicationMessage;

            if (_eventContainer.InterceptingPublishEvent.HasHandlers)
            {
                var interceptingPublishEventArgs = new InterceptingPublishEventArgs(
                    applicationMessage,
                    _cancellationTokenSource.Token,
                    injectedApplicationMessage.SenderClientId,
                    _sessionItems);

                await _eventContainer.InterceptingPublishEvent.InvokeAsync(interceptingPublishEventArgs).ConfigureAwait(false);

                applicationMessage = interceptingPublishEventArgs.ApplicationMessage;
                processPublish = interceptingPublishEventArgs.ProcessPublish;
            }

            if (!processPublish)
            {
                return;
            }
            
            if (string.IsNullOrEmpty(applicationMessage.Topic))
            {
                throw new NotSupportedException("Injected application messages must contain a topic. Topic alias is not supported.");
            }
            
            await _clientSessionsManager.DispatchApplicationMessage(injectedApplicationMessage.SenderClientId, applicationMessage).ConfigureAwait(false);
        }

        public async Task StartAsync()
        {
            ThrowIfStarted();

            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            await _retainedMessagesManager.Start().ConfigureAwait(false);
            _clientSessionsManager.Start();
            _keepAliveMonitor.Start(cancellationToken);

            foreach (var adapter in _adapters)
            {
                adapter.ClientHandler = c => OnHandleClient(c, cancellationToken);
                await adapter.StartAsync(_options, _rootLogger).ConfigureAwait(false);
            }

            await _eventContainer.StartedEvent.InvokeAsync(EventArgs.Empty).ConfigureAwait(false);

            _logger.Info("Started.");
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

                await _clientSessionsManager.Stop().ConfigureAwait(false);
                await _retainedMessagesManager.Stop().ConfigureAwait(false);
                
                foreach (var adapter in _adapters)
                {
                    adapter.ClientHandler = null;
                    await adapter.StopAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }

            await _eventContainer.StoppedEvent.InvokeAsync(EventArgs.Empty).ConfigureAwait(false);

            _logger.Info("Stopped.");
        }

        public Task SubscribeAsync(string clientId, ICollection<MqttTopicFilter> topicFilters)
        {
            if (clientId == null)
            {
                throw new ArgumentNullException(nameof(clientId));
            }

            if (topicFilters == null)
            {
                throw new ArgumentNullException(nameof(topicFilters));
            }

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
            if (clientId == null)
            {
                throw new ArgumentNullException(nameof(clientId));
            }

            if (topicFilters == null)
            {
                throw new ArgumentNullException(nameof(topicFilters));
            }

            ThrowIfDisposed();
            ThrowIfNotStarted();

            return _clientSessionsManager.UnsubscribeAsync(clientId, topicFilters);
        }

        public Task UpdateRetainedMessageAsync(MqttApplicationMessage retainedMessage)
        {
            if (retainedMessage == null)
            {
                throw new ArgumentNullException(nameof(retainedMessage));
            }

            ThrowIfDisposed();
            ThrowIfNotStarted();

            return _retainedMessagesManager?.HandleMessage(null, retainedMessage);
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

        void ThrowIfNotStarted()
        {
            ThrowIfDisposed();

            if (_cancellationTokenSource == null)
            {
                throw new InvalidOperationException("The MQTT server is not started.");
            }
        }

        void ThrowIfStarted()
        {
            ThrowIfDisposed();

            if (_cancellationTokenSource != null)
            {
                throw new InvalidOperationException("The MQTT server is already started.");
            }
        }
    }
}