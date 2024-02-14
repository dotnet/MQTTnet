// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace MQTTnet.Extensions.ManagedClient
{
    public sealed class ManagedMqttClient : Disposable, IManagedMqttClient
    {
        readonly AsyncEvent<InterceptingPublishMessageEventArgs> _interceptingPublishMessageEvent = new AsyncEvent<InterceptingPublishMessageEventArgs>();
        readonly AsyncEvent<ApplicationMessageProcessedEventArgs> _applicationMessageProcessedEvent = new AsyncEvent<ApplicationMessageProcessedEventArgs>();
        readonly AsyncEvent<ApplicationMessageSkippedEventArgs> _applicationMessageSkippedEvent = new AsyncEvent<ApplicationMessageSkippedEventArgs>();
        readonly AsyncEvent<ConnectingFailedEventArgs> _connectingFailedEvent = new AsyncEvent<ConnectingFailedEventArgs>();
        readonly AsyncEvent<EventArgs> _connectionStateChangedEvent = new AsyncEvent<EventArgs>();
        readonly AsyncEvent<ManagedProcessFailedEventArgs> _synchronizingSubscriptionsFailedEvent = new AsyncEvent<ManagedProcessFailedEventArgs>();
        
        readonly MqttNetSourceLogger _logger;
        readonly BlockingQueue<ManagedMqttApplicationMessage> _messageQueue = new BlockingQueue<ManagedMqttApplicationMessage>();

        readonly AsyncLock _messageQueueLock = new AsyncLock();

        /// <summary>
        ///     The subscriptions are managed in 2 separate buckets:
        ///     <see
        ///         cref="_subscriptions" />
        ///     and
        ///     <see
        ///         cref="_unsubscriptions" />
        ///     are processed during normal operation
        ///     and are moved to the
        ///     <see
        ///         cref="_reconnectSubscriptions" />
        ///     when they get processed. They can be accessed by
        ///     any thread and are therefore mutex'ed.
        ///     <see
        ///         cref="_reconnectSubscriptions" />
        ///     get sent to the broker
        ///     at reconnect and are solely owned by
        ///     <see
        ///         cref="MaintainConnectionAsync" />
        ///     .
        /// </summary>
        readonly Dictionary<string, MqttTopicFilter> _reconnectSubscriptions = new Dictionary<string, MqttTopicFilter>();

        readonly Dictionary<string, MqttTopicFilter> _subscriptions = new Dictionary<string, MqttTopicFilter>();
        readonly SemaphoreSlim _subscriptionsQueuedSignal = new SemaphoreSlim(0);
        readonly HashSet<string> _unsubscriptions = new HashSet<string>();

        CancellationTokenSource _connectionCancellationToken;
        Task _maintainConnectionTask;
        CancellationTokenSource _publishingCancellationToken;

        ManagedMqttClientStorageManager _storageManager;
        bool _isCleanDisconnect;

        public ManagedMqttClient(IMqttClient mqttClient, IMqttNetLogger logger)
        {
            InternalClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _logger = logger.WithSource(nameof(ManagedMqttClient));
        }

        public event Func<ApplicationMessageSkippedEventArgs, Task> ApplicationMessageSkippedAsync
        {
            add => _applicationMessageSkippedEvent.AddHandler(value);
            remove => _applicationMessageSkippedEvent.RemoveHandler(value);
        }
        
        public event Func<ApplicationMessageProcessedEventArgs, Task> ApplicationMessageProcessedAsync
        {
            add => _applicationMessageProcessedEvent.AddHandler(value);
            remove => _applicationMessageProcessedEvent.RemoveHandler(value);
        }


        public event Func<InterceptingPublishMessageEventArgs, Task> InterceptPublishMessageAsync
        {
            add => _interceptingPublishMessageEvent.AddHandler(value);
            remove => _interceptingPublishMessageEvent.RemoveHandler(value);
        }


        public event Func<MqttApplicationMessageReceivedEventArgs, Task> ApplicationMessageReceivedAsync
        {
            add => InternalClient.ApplicationMessageReceivedAsync += value;
            remove => InternalClient.ApplicationMessageReceivedAsync -= value;
        }

        public event Func<MqttClientConnectedEventArgs, Task> ConnectedAsync
        {
            add => InternalClient.ConnectedAsync += value;
            remove => InternalClient.ConnectedAsync -= value;
        }

        public event Func<ConnectingFailedEventArgs, Task> ConnectingFailedAsync
        {
            add => _connectingFailedEvent.AddHandler(value);
            remove => _connectingFailedEvent.RemoveHandler(value);
        }

        public event Func<EventArgs, Task> ConnectionStateChangedAsync
        {
            add => _connectionStateChangedEvent.AddHandler(value);
            remove => _connectionStateChangedEvent.RemoveHandler(value);
        }

        public event Func<MqttClientDisconnectedEventArgs, Task> DisconnectedAsync
        {
            add => InternalClient.DisconnectedAsync += value;
            remove => InternalClient.DisconnectedAsync -= value;
        }

        public event Func<ManagedProcessFailedEventArgs, Task> SynchronizingSubscriptionsFailedAsync
        {
            add => _synchronizingSubscriptionsFailedEvent.AddHandler(value);
            remove => _synchronizingSubscriptionsFailedEvent.RemoveHandler(value);
        }

        public IMqttClient InternalClient { get; }

        public bool IsConnected => InternalClient.IsConnected;

        public bool IsStarted => _connectionCancellationToken != null;

        public ManagedMqttClientOptions Options { get; private set; }

        public int PendingApplicationMessagesCount => _messageQueue.Count;
        
        public async Task EnqueueAsync(MqttApplicationMessage applicationMessage)
        {
            ThrowIfDisposed();

            if (applicationMessage == null)
            {
                throw new ArgumentNullException(nameof(applicationMessage));
            }

            var managedMqttApplicationMessage = new ManagedMqttApplicationMessageBuilder().WithApplicationMessage(applicationMessage);
            await EnqueueAsync(managedMqttApplicationMessage.Build()).ConfigureAwait(false);
        }

        public async Task EnqueueAsync(ManagedMqttApplicationMessage applicationMessage)
        {
            ThrowIfDisposed();

            if (applicationMessage == null)
            {
                throw new ArgumentNullException(nameof(applicationMessage));
            }

            if (Options == null)
            {
                throw new InvalidOperationException("call StartAsync before publishing messages");
            }

            MqttTopicValidator.ThrowIfInvalid(applicationMessage.ApplicationMessage);

            ManagedMqttApplicationMessage removedMessage = null;
            ApplicationMessageSkippedEventArgs applicationMessageSkippedEventArgs = null;

            try
            {
                using (await _messageQueueLock.EnterAsync().ConfigureAwait(false))
                {
                    if (_messageQueue.Count >= Options.MaxPendingMessages)
                    {
                        if (Options.PendingMessagesOverflowStrategy == MqttPendingMessagesOverflowStrategy.DropNewMessage)
                        {
                            _logger.Verbose("Skipping publish of new application message because internal queue is full.");
                            applicationMessageSkippedEventArgs = new ApplicationMessageSkippedEventArgs(applicationMessage);
                            return;
                        }

                        if (Options.PendingMessagesOverflowStrategy == MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage)
                        {
                            removedMessage = _messageQueue.RemoveFirst();
                            _logger.Verbose("Removed oldest application message from internal queue because it is full.");
                            applicationMessageSkippedEventArgs = new ApplicationMessageSkippedEventArgs(removedMessage);
                        }
                    }

                    _messageQueue.Enqueue(applicationMessage);

                    if (_storageManager != null)
                    {
                        if (removedMessage != null)
                        {
                            await _storageManager.RemoveAsync(removedMessage).ConfigureAwait(false);
                        }

                        await _storageManager.AddAsync(applicationMessage).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                if (applicationMessageSkippedEventArgs != null && _applicationMessageSkippedEvent.HasHandlers)
                {
                    await _applicationMessageSkippedEvent.InvokeAsync(applicationMessageSkippedEventArgs).ConfigureAwait(false);
                }
            }
        }

        public Task PingAsync(CancellationToken cancellationToken = default)
        {
            return InternalClient.PingAsync(cancellationToken);
        }

        public async Task StartAsync(ManagedMqttClientOptions options)
        {
            ThrowIfDisposed();

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.ClientOptions == null)
            {
                throw new ArgumentException("The client options are not set.", nameof(options));
            }

            if (!_maintainConnectionTask?.IsCompleted ?? false)
            {
                throw new InvalidOperationException("The managed client is already started.");
            }

            Options = options;

            if (options.Storage != null)
            {
                _storageManager = new ManagedMqttClientStorageManager(options.Storage);
                var messages = await _storageManager.LoadQueuedMessagesAsync().ConfigureAwait(false);

                foreach (var message in messages)
                {
                    _messageQueue.Enqueue(message);
                }
            }

            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            _connectionCancellationToken = cancellationTokenSource;

            _maintainConnectionTask = Task.Run(() => MaintainConnectionAsync(cancellationToken), cancellationToken);
            _maintainConnectionTask.RunInBackground(_logger);

            _logger.Info("Started");
        }

        public async Task StopAsync(bool cleanDisconnect = true)
        {
            ThrowIfDisposed();

            _isCleanDisconnect = cleanDisconnect;
            
            StopPublishing();
            StopMaintainingConnection();

            _messageQueue.Clear();

            if (_maintainConnectionTask != null)
            {
                await Task.WhenAny(_maintainConnectionTask);
                _maintainConnectionTask = null;
            }
        }

        public Task SubscribeAsync(ICollection<MqttTopicFilter> topicFilters)
        {
            ThrowIfDisposed();

            if (topicFilters == null)
            {
                throw new ArgumentNullException(nameof(topicFilters));
            }

            foreach (var topicFilter in topicFilters)
            {
                MqttTopicValidator.ThrowIfInvalidSubscribe(topicFilter.Topic);
            }

            lock (_subscriptions)
            {
                foreach (var topicFilter in topicFilters)
                {
                    _subscriptions[topicFilter.Topic] = topicFilter;
                    _unsubscriptions.Remove(topicFilter.Topic);
                }
            }

            _subscriptionsQueuedSignal.Release();

            return CompletedTask.Instance;
        }

        public Task UnsubscribeAsync(ICollection<string> topics)
        {
            ThrowIfDisposed();

            if (topics == null)
            {
                throw new ArgumentNullException(nameof(topics));
            }

            lock (_subscriptions)
            {
                foreach (var topic in topics)
                {
                    _subscriptions.Remove(topic);
                    _unsubscriptions.Add(topic);
                }
            }

            _subscriptionsQueuedSignal.Release();

            return CompletedTask.Instance;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopPublishing();
                StopMaintainingConnection();

                if (_maintainConnectionTask != null)
                {
                    _maintainConnectionTask.GetAwaiter().GetResult();
                    _maintainConnectionTask = null;
                }

                _messageQueue.Dispose();
                _messageQueueLock.Dispose();
                InternalClient.Dispose();
                _subscriptionsQueuedSignal.Dispose();
            }

            base.Dispose(disposing);
        }

        static TimeSpan GetRemainingTime(DateTime endTime)
        {
            var remainingTime = endTime - DateTime.UtcNow;
            return remainingTime < TimeSpan.Zero ? TimeSpan.Zero : remainingTime;
        }

        async Task HandleSubscriptionExceptionAsync(Exception exception, List<MqttTopicFilter> addedSubscriptions, List<string> removedSubscriptions)
        {
            _logger.Warning(exception, "Synchronizing subscriptions failed.");

            if (_synchronizingSubscriptionsFailedEvent.HasHandlers)
            {
                await _synchronizingSubscriptionsFailedEvent.InvokeAsync(new ManagedProcessFailedEventArgs(exception, addedSubscriptions, removedSubscriptions)).ConfigureAwait(false);
            }
        }

        async Task MaintainConnectionAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await TryMaintainConnectionAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Error exception while maintaining connection.");
            }
            finally
            {
                if (!IsDisposed)
                {
                    try
                    {
                        if (_isCleanDisconnect)
                        {
                            using (var disconnectTimeout = new CancellationTokenSource(Options.ClientOptions.Timeout))
                            {
                                await InternalClient.DisconnectAsync(new MqttClientDisconnectOptions(), disconnectTimeout.Token).ConfigureAwait(false);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.Warning("Timeout while sending DISCONNECT packet.");
                    }
                    catch (Exception exception)
                    {
                        _logger.Error(exception, "Error while disconnecting.");
                    }

                    _logger.Info("Stopped");
                }

                _reconnectSubscriptions.Clear();

                lock (_subscriptions)
                {
                    _subscriptions.Clear();
                    _unsubscriptions.Clear();
                }
            }
        }

        async Task PublishQueuedMessagesAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && InternalClient.IsConnected)
                {
                    // Peek at the message without dequeueing in order to prevent the
                    // possibility of the queue growing beyond the configured cap.
                    // Previously, messages could be re-enqueued if there was an
                    // exception, and this re-enqueueing did not honor the cap.
                    // Furthermore, because re-enqueueing would shuffle the order
                    // of the messages, the DropOldestQueuedMessage strategy would
                    // be unable to know which message is actually the oldest and would
                    // instead drop the first item in the queue.
                    var message = _messageQueue.PeekAndWait(cancellationToken);
                    if (message == null)
                    {
                        continue;
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    await TryPublishQueuedMessageAsync(message).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Error while publishing queued application messages.");
            }
            finally
            {
                _logger.Verbose("Stopped publishing messages.");
            }
        }

        async Task PublishReconnectSubscriptionsAsync()
        {
            _logger.Info("Publishing subscriptions at reconnect");

            List<MqttTopicFilter> topicFilters = null;

            try
            {
                if (_reconnectSubscriptions.Any())
                {
                    topicFilters = new List<MqttTopicFilter>();

                    foreach (var sub in _reconnectSubscriptions)
                    {
                        topicFilters.Add(sub.Value);

                        if (topicFilters.Count == Options.MaxTopicFiltersInSubscribeUnsubscribePackets)
                        {
                            await SendSubscribeUnsubscribe(topicFilters, null).ConfigureAwait(false);
                            topicFilters.Clear();
                        }
                    }

                    await SendSubscribeUnsubscribe(topicFilters, null).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                await HandleSubscriptionExceptionAsync(exception, topicFilters, null).ConfigureAwait(false);
            }
        }

        async Task PublishSubscriptionsAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var endTime = DateTime.UtcNow + timeout;

            while (await _subscriptionsQueuedSignal.WaitAsync(GetRemainingTime(endTime), cancellationToken).ConfigureAwait(false))
            {
                List<MqttTopicFilter> subscriptions;
                HashSet<string> unsubscriptions;

                lock (_subscriptions)
                {
                    subscriptions = _subscriptions.Values.ToList();
                    _subscriptions.Clear();

                    unsubscriptions = new HashSet<string>(_unsubscriptions);
                    _unsubscriptions.Clear();
                }

                if (!subscriptions.Any() && !unsubscriptions.Any())
                {
                    continue;
                }

                _logger.Verbose("Publishing {0} added and {1} removed subscriptions", subscriptions.Count, unsubscriptions.Count);

                foreach (var unsubscription in unsubscriptions)
                {
                    _reconnectSubscriptions.Remove(unsubscription);
                }

                foreach (var subscription in subscriptions)
                {
                    _reconnectSubscriptions[subscription.Topic] = subscription;
                }

                var addedTopicFilters = new List<MqttTopicFilter>();
                foreach (var subscription in subscriptions)
                {
                    addedTopicFilters.Add(subscription);

                    if (addedTopicFilters.Count == Options.MaxTopicFiltersInSubscribeUnsubscribePackets)
                    {
                        await SendSubscribeUnsubscribe(addedTopicFilters, null).ConfigureAwait(false);
                        addedTopicFilters.Clear();
                    }
                }

                await SendSubscribeUnsubscribe(addedTopicFilters, null).ConfigureAwait(false);

                var removedTopicFilters = new List<string>();
                foreach (var unSub in unsubscriptions)
                {
                    removedTopicFilters.Add(unSub);

                    if (removedTopicFilters.Count == Options.MaxTopicFiltersInSubscribeUnsubscribePackets)
                    {
                        await SendSubscribeUnsubscribe(null, removedTopicFilters).ConfigureAwait(false);
                        removedTopicFilters.Clear();
                    }
                }

                await SendSubscribeUnsubscribe(null, removedTopicFilters).ConfigureAwait(false);
            }
        }

        async Task<ReconnectionResult> ReconnectIfRequiredAsync(CancellationToken cancellationToken)
        {
            if (InternalClient.IsConnected)
            {
                return ReconnectionResult.StillConnected;
            }

            MqttClientConnectResult connectResult = null;
            try
            {
                using (var connectTimeout = new CancellationTokenSource(Options.ClientOptions.Timeout))
                {
                    connectResult = await InternalClient.ConnectAsync(Options.ClientOptions, connectTimeout.Token).ConfigureAwait(false);
                }

                if (connectResult.ResultCode != MqttClientConnectResultCode.Success)
                {
                    throw new MqttCommunicationException($"Client connected but server denied connection with reason '{connectResult.ResultCode}'.");
                }

                return connectResult.IsSessionPresent ? ReconnectionResult.Recovered : ReconnectionResult.Reconnected;
            }
            catch (Exception exception)
            {
                await _connectingFailedEvent.InvokeAsync(new ConnectingFailedEventArgs(connectResult, exception));
                return ReconnectionResult.NotConnected;
            }
        }

        async Task SendSubscribeUnsubscribe(List<MqttTopicFilter> addedSubscriptions, List<string> removedSubscriptions)
        {
            try
            {
                if (removedSubscriptions != null && removedSubscriptions.Any())
                {
                    var unsubscribeOptionsBuilder = new MqttClientUnsubscribeOptionsBuilder();

                    foreach (var removedSubscription in removedSubscriptions)
                    {
                        unsubscribeOptionsBuilder.WithTopicFilter(removedSubscription);
                    }

                    await InternalClient.UnsubscribeAsync(unsubscribeOptionsBuilder.Build()).ConfigureAwait(false);

                    //clear because these worked, maybe the subscribe below will fail, only report those
                    removedSubscriptions.Clear();
                }

                if (addedSubscriptions != null && addedSubscriptions.Any())
                {
                    var subscribeOptionsBuilder = new MqttClientSubscribeOptionsBuilder();

                    foreach (var addedSubscription in addedSubscriptions)
                    {
                        subscribeOptionsBuilder.WithTopicFilter(addedSubscription);
                    }

                    await InternalClient.SubscribeAsync(subscribeOptionsBuilder.Build()).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                await HandleSubscriptionExceptionAsync(exception, addedSubscriptions, removedSubscriptions).ConfigureAwait(false);
            }
        }

        void StartPublishing()
        {
            StopPublishing();

            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            _publishingCancellationToken = cancellationTokenSource;

            Task.Run(() => PublishQueuedMessagesAsync(cancellationToken), cancellationToken).RunInBackground(_logger);
        }

        void StopMaintainingConnection()
        {
            try
            {
                _connectionCancellationToken?.Cancel(false);
            }
            finally
            {
                _connectionCancellationToken?.Dispose();
                _connectionCancellationToken = null;
            }
        }

        void StopPublishing()
        {
            try
            {
                _publishingCancellationToken?.Cancel(false);
            }
            finally
            {
                _publishingCancellationToken?.Dispose();
                _publishingCancellationToken = null;
            }
        }

        async Task TryMaintainConnectionAsync(CancellationToken cancellationToken)
        {
            try
            {
                var oldConnectionState = InternalClient.IsConnected;
                var connectionState = await ReconnectIfRequiredAsync(cancellationToken).ConfigureAwait(false);
                
                if (connectionState == ReconnectionResult.NotConnected)
                {
                    StopPublishing();
                    await Task.Delay(Options.AutoReconnectDelay, cancellationToken).ConfigureAwait(false);
                }
                else if (connectionState == ReconnectionResult.Reconnected)
                {
                    await PublishReconnectSubscriptionsAsync().ConfigureAwait(false);
                    StartPublishing();
                }
                else if (connectionState == ReconnectionResult.Recovered)
                {
                    StartPublishing();
                }
                else if (connectionState == ReconnectionResult.StillConnected)
                {
                    await PublishSubscriptionsAsync(Options.ConnectionCheckInterval, cancellationToken).ConfigureAwait(false);
                }

                if (oldConnectionState != InternalClient.IsConnected)
                {
                    await _connectionStateChangedEvent.InvokeAsync(EventArgs.Empty).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (MqttCommunicationException exception)
            {
                _logger.Warning(exception, "Communication error while maintaining connection.");
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Error exception while maintaining connection.");
            }
        }

        async Task TryPublishQueuedMessageAsync(ManagedMqttApplicationMessage message)
        {
            Exception transmitException = null;
            bool acceptPublish = true;
            try
            {
                if (_interceptingPublishMessageEvent.HasHandlers)
                {
                    var interceptEventArgs = new InterceptingPublishMessageEventArgs(message);
                    await _interceptingPublishMessageEvent.InvokeAsync(interceptEventArgs).ConfigureAwait(false);
                    acceptPublish = interceptEventArgs.AcceptPublish;
                }

                if (acceptPublish)
                {
                    await InternalClient.PublishAsync(message.ApplicationMessage).ConfigureAwait(false);
                }

                using (await _messageQueueLock.EnterAsync().ConfigureAwait(false)) //lock to avoid conflict with this.PublishAsync
                {
                    // While publishing this message, this.PublishAsync could have booted this
                    // message off the queue to make room for another (when using a cap
                    // with the DropOldestQueuedMessage strategy).  If the first item
                    // in the queue is equal to this message, then it's safe to remove
                    // it from the queue.  If not, that means this.PublishAsync has already
                    // removed it, in which case we don't want to do anything.
                    _messageQueue.RemoveFirst(i => i.Id.Equals(message.Id));

                    if (_storageManager != null)
                    {
                        await _storageManager.RemoveAsync(message).ConfigureAwait(false);
                    }
                }
            }
            catch (MqttCommunicationException exception)
            {
                transmitException = exception;

                _logger.Warning(exception, "Publishing application message ({0}) failed.", message.Id);

                if (message.ApplicationMessage.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtMostOnce)
                {
                    //If QoS 0, we don't want this message to stay on the queue.
                    //If QoS 1 or 2, it's possible that, when using a cap, this message
                    //has been booted off the queue by this.PublishAsync, in which case this
                    //thread will not continue to try to publish it. While this does
                    //contradict the expected behavior of QoS 1 and 2, that's also true
                    //for the usage of a message queue cap, so it's still consistent
                    //with prior behavior in that way.
                    using (await _messageQueueLock.EnterAsync().ConfigureAwait(false)) //lock to avoid conflict with this.PublishAsync
                    {
                        _messageQueue.RemoveFirst(i => i.Id.Equals(message.Id));

                        if (_storageManager != null)
                        {
                            await _storageManager.RemoveAsync(message).ConfigureAwait(false);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                transmitException = exception;
                _logger.Error(exception, "Error while publishing application message ({0}).", message.Id);
            }
            finally
            {
                if (_applicationMessageProcessedEvent.HasHandlers)
                {
                    var eventArgs = new ApplicationMessageProcessedEventArgs(message, transmitException);
                    await _applicationMessageProcessedEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
                }
            }
        }
    }
}