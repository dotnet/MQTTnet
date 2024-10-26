// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Concurrent;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server.Internal;

namespace MQTTnet.Server;

public class MqttServer : Disposable
{
    readonly ICollection<IMqttServerAdapter> _adapters;
    readonly MqttClientSessionsManager _clientSessionsManager;
    readonly MqttServerEventContainer _eventContainer = new();
    readonly MqttServerKeepAliveMonitor _keepAliveMonitor;
    readonly MqttNetSourceLogger _logger;
    readonly MqttServerOptions _options;
    readonly MqttRetainedMessagesManager _retainedMessagesManager;
    readonly IMqttNetLogger _rootLogger;

    CancellationTokenSource _cancellationTokenSource;
    bool _isStopping;

    public MqttServer(MqttServerOptions options, IEnumerable<IMqttServerAdapter> adapters, IMqttNetLogger logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));

        ArgumentNullException.ThrowIfNull(adapters);

        _adapters = adapters.ToList();

        _rootLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger = logger.WithSource(nameof(MqttServer));

        _retainedMessagesManager = new MqttRetainedMessagesManager(_eventContainer, _rootLogger);
        _clientSessionsManager = new MqttClientSessionsManager(options, _retainedMessagesManager, _eventContainer, _rootLogger);
        _keepAliveMonitor = new MqttServerKeepAliveMonitor(options, _clientSessionsManager, _rootLogger);
    }

    public event Func<ApplicationMessageEnqueuedEventArgs, Task> ApplicationMessageEnqueuedOrDroppedAsync
    {
        add => _eventContainer.ApplicationMessageEnqueuedOrDroppedEvent.AddHandler(value);
        remove => _eventContainer.ApplicationMessageEnqueuedOrDroppedEvent.RemoveHandler(value);
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

    public event Func<InterceptingClientApplicationMessageEnqueueEventArgs, Task> InterceptingClientEnqueueAsync
    {
        add => _eventContainer.InterceptingClientEnqueueEvent.AddHandler(value);
        remove => _eventContainer.InterceptingClientEnqueueEvent.RemoveHandler(value);
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

    public event Func<QueueMessageOverwrittenEventArgs, Task> QueuedApplicationMessageOverwrittenAsync
    {
        add => _eventContainer.QueuedApplicationMessageOverwrittenEvent.AddHandler(value);
        remove => _eventContainer.QueuedApplicationMessageOverwrittenEvent.RemoveHandler(value);
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

    /// <summary>
    ///     Gets or sets whether the server will accept new connections.
    ///     If not, the server will close the connection without any notification (DISCONNECT packet).
    ///     This feature can be used when the server is shutting down.
    /// </summary>
    public bool AcceptNewConnections { get; set; } = true;

    public bool IsStarted => _cancellationTokenSource != null;

    /// <summary>
    ///     Gives access to the session items which belong to this server. This session items are passed
    ///     to several events instead of the client session items if the event is caused by the server instead of a client.
    /// </summary>
    public IDictionary ServerSessionItems { get; } = new ConcurrentDictionary<object, object>();

    public Task DeleteRetainedMessagesAsync()
    {
        ThrowIfNotStarted();

        return _retainedMessagesManager?.ClearMessages() ?? CompletedTask.Instance;
    }

    public Task DisconnectClientAsync(string id, MqttServerClientDisconnectOptions options)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(options);

        ThrowIfNotStarted();

        return _clientSessionsManager.GetClient(id).StopAsync(options);
    }

    public Task<IList<MqttClientStatus>> GetClientsAsync()
    {
        ThrowIfNotStarted();

        return _clientSessionsManager.GetClientsStatus();
    }

    public Task<MqttApplicationMessage> GetRetainedMessageAsync(string topic)
    {
        ArgumentNullException.ThrowIfNull(topic);

        ThrowIfNotStarted();

        return _retainedMessagesManager.GetMessage(topic);
    }

    public Task<IList<MqttApplicationMessage>> GetRetainedMessagesAsync()
    {
        ThrowIfNotStarted();

        return _retainedMessagesManager.GetMessages();
    }

    public Task<IList<MqttSessionStatus>> GetSessionsAsync()
    {
        ThrowIfNotStarted();

        return _clientSessionsManager.GetSessionsStatus();
    }

    public Task InjectApplicationMessage(InjectedMqttApplicationMessage injectedApplicationMessage, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(injectedApplicationMessage);
        ArgumentNullException.ThrowIfNull(injectedApplicationMessage.ApplicationMessage);

        MqttTopicValidator.ThrowIfInvalid(injectedApplicationMessage.ApplicationMessage.Topic);

        ThrowIfNotStarted();

        if (string.IsNullOrEmpty(injectedApplicationMessage.ApplicationMessage.Topic))
        {
            throw new NotSupportedException("Injected application messages must contain a topic (topic alias is not supported)");
        }

        var sessionItems = injectedApplicationMessage.CustomSessionItems ?? ServerSessionItems;

        return _clientSessionsManager.DispatchApplicationMessage(
            injectedApplicationMessage.SenderClientId,
            sessionItems,
            injectedApplicationMessage.ApplicationMessage,
            cancellationToken);
    }

    public async Task StartAsync()
    {
        ThrowIfStarted();

        _isStopping = false;

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

        _logger.Info("Started");
    }

    public async Task StopAsync(MqttServerStopOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            if (_cancellationTokenSource == null)
            {
                return;
            }

            _isStopping = true;

            _cancellationTokenSource.Cancel(false);

            await _clientSessionsManager.CloseAllConnections(options.DefaultClientDisconnectOptions).ConfigureAwait(false);

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

        _logger.Info("Stopped");
    }

    public Task SubscribeAsync(string clientId, ICollection<MqttTopicFilter> topicFilters)
    {
        ArgumentNullException.ThrowIfNull(clientId);
        ArgumentNullException.ThrowIfNull(topicFilters);

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
        ArgumentNullException.ThrowIfNull(clientId);
        ArgumentNullException.ThrowIfNull(topicFilters);

        ThrowIfDisposed();
        ThrowIfNotStarted();

        return _clientSessionsManager.UnsubscribeAsync(clientId, topicFilters);
    }

    public Task UpdateRetainedMessageAsync(MqttApplicationMessage retainedMessage)
    {
        ArgumentNullException.ThrowIfNull(retainedMessage);

        ThrowIfDisposed();
        ThrowIfNotStarted();

        return _retainedMessagesManager?.UpdateMessage(string.Empty, retainedMessage);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopAsync(new MqttServerStopOptions()).GetAwaiter().GetResult();

            foreach (var adapter in _adapters)
            {
                adapter.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    Task OnHandleClient(IMqttChannelAdapter channelAdapter, CancellationToken cancellationToken)
    {
        if (_isStopping || !AcceptNewConnections)
        {
            return CompletedTask.Instance;
        }

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