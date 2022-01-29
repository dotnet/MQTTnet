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
using MqttClient = MQTTnet.Client.MqttClient;

namespace MQTTnet.Extensions.ManagedClient
{
    public sealed class ManagedMqttClient : Disposable
    {
        readonly MqttNetSourceLogger _logger;
        readonly BlockingQueue<ManagedMqttApplicationMessage> _messageQueue = new BlockingQueue<ManagedMqttApplicationMessage>();

        readonly AsyncLock _messageQueueLock = new AsyncLock();

        /// <summary>
        ///     The subscriptions are managed in 2 separate buckets:
        ///     <see cref="_subscriptions" /> and <see cref="_unsubscriptions" /> are processed during normal operation
        ///     and are moved to the <see cref="_reconnectSubscriptions" /> when they get processed. They can be accessed by
        ///     any thread and are therefore mutex'ed. <see cref="_reconnectSubscriptions" /> get sent to the broker
        ///     at reconnect and are solely owned by <see cref="MaintainConnectionAsync" />.
        /// </summary>
        readonly Dictionary<string, MqttQualityOfServiceLevel> _reconnectSubscriptions = new Dictionary<string, MqttQualityOfServiceLevel>();

        readonly Dictionary<string, MqttQualityOfServiceLevel> _subscriptions = new Dictionary<string, MqttQualityOfServiceLevel>();
        readonly SemaphoreSlim _subscriptionsQueuedSignal = new SemaphoreSlim(0);
        readonly HashSet<string> _unsubscriptions = new HashSet<string>();

        CancellationTokenSource _connectionCancellationToken;
        Task _maintainConnectionTask;
        CancellationTokenSource _publishingCancellationToken;

        ManagedMqttClientStorageManager _storageManager;

        public ManagedMqttClient(MqttClient mqttClient, IMqttNetLogger logger)
        {
            InternalClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _logger = logger.WithSource(nameof(ManagedMqttClient));
        }

        public event Func<MqttApplicationMessageReceivedEventArgs, Task> ApplicationMessageReceivedAsync
        {
            add => InternalClient.ApplicationMessageReceivedAsync += value;
            remove => InternalClient.ApplicationMessageReceivedAsync -= value;
        }

        public event Func<EventArgs, Task> ConnectedAsync
        {
            add => InternalClient.ConnectedAsync += value;
            remove => InternalClient.ConnectedAsync -= value;
        }

        public event Func<EventArgs, Task> DisconnectedAsync
        {
            add => InternalClient.DisconnectedAsync += value;
            remove => InternalClient.DisconnectedAsync -= value;
        }

        public bool IsConnected => InternalClient.IsConnected;

        public bool IsStarted => _connectionCancellationToken != null;

        public MqttClient InternalClient { get; }

        public int PendingApplicationMessagesCount => _messageQueue.Count;

        public IManagedMqttClientOptions Options { get; private set; }

        public IApplicationMessageProcessedHandler ApplicationMessageProcessedHandler { get; set; }

        public IApplicationMessageSkippedHandler ApplicationMessageSkippedHandler { get; set; }

        public IConnectingFailedHandler ConnectingFailedHandler { get; set; }

        public ISynchronizingSubscriptionsFailedHandler SynchronizingSubscriptionsFailedHandler { get; set; }

        public Task PingAsync(CancellationToken cancellationToken)
        {
            return InternalClient.PingAsync(cancellationToken);
        }

        public async Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (applicationMessage == null)
            {
                throw new ArgumentNullException(nameof(applicationMessage));
            }

            await PublishAsync(
                    new ManagedMqttApplicationMessageBuilder().WithApplicationMessage(applicationMessage)
                        .Build())
                .ConfigureAwait(false);
            return new MqttClientPublishResult();
        }

        public async Task PublishAsync(ManagedMqttApplicationMessage applicationMessage)
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

            MqttTopicValidator.ThrowIfInvalid(applicationMessage.ApplicationMessage.Topic);

            ManagedMqttApplicationMessage removedMessage = null;
            ApplicationMessageSkippedEventArgs applicationMessageSkippedEventArgs = null;

            try
            {
                using (await _messageQueueLock.WaitAsync(CancellationToken.None)
                           .ConfigureAwait(false))
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
                            await _storageManager.RemoveAsync(removedMessage)
                                .ConfigureAwait(false);
                        }

                        await _storageManager.AddAsync(applicationMessage)
                            .ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                if (applicationMessageSkippedEventArgs != null)
                {
                    var applicationMessageSkippedHandler = ApplicationMessageSkippedHandler;
                    if (applicationMessageSkippedHandler != null)
                    {
                        await applicationMessageSkippedHandler.HandleApplicationMessageSkippedAsync(applicationMessageSkippedEventArgs)
                            .ConfigureAwait(false);
                    }
                }
            }
        }

        public async Task StartAsync(IManagedMqttClientOptions options)
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
                var messages = await _storageManager.LoadQueuedMessagesAsync()
                    .ConfigureAwait(false);

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

        public async Task StopAsync()
        {
            ThrowIfDisposed();

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
                    _subscriptions[topicFilter.Topic] = topicFilter.QualityOfServiceLevel;
                    _unsubscriptions.Remove(topicFilter.Topic);
                }
            }

            _subscriptionsQueuedSignal.Release();

            return Task.FromResult(0);
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

            return Task.FromResult(0);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopPublishing();
                StopMaintainingConnection();

                if (_maintainConnectionTask != null)
                {
                    _maintainConnectionTask.GetAwaiter()
                        .GetResult();
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

        async Task HandleSubscriptionExceptionAsync(Exception exception)
        {
            _logger.Warning(exception, "Synchronizing subscriptions failed.");

            var synchronizingSubscriptionsFailedHandler = SynchronizingSubscriptionsFailedHandler;
            if (SynchronizingSubscriptionsFailedHandler != null)
            {
                await synchronizingSubscriptionsFailedHandler.HandleSynchronizingSubscriptionsFailedAsync(new ManagedProcessFailedEventArgs(exception))
                    .ConfigureAwait(false);
            }
        }

        async Task MaintainConnectionAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await TryMaintainConnectionAsync(cancellationToken)
                        .ConfigureAwait(false);
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
                        using (var disconnectTimeout = new CancellationTokenSource(Options.ClientOptions.CommunicationTimeout))
                        {
                            await InternalClient.DisconnectAsync(new MqttClientDisconnectOptions(), disconnectTimeout.Token)
                                .ConfigureAwait(false);
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

                    await TryPublishQueuedMessageAsync(message)
                        .ConfigureAwait(false);
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

            try
            {
                if (_reconnectSubscriptions.Any())
                {
                    var subscriptions = _reconnectSubscriptions.Select(
                        i => new MqttTopicFilter
                        {
                            Topic = i.Key,
                            QualityOfServiceLevel = i.Value
                        });

                    var topicFilters = new List<MqttTopicFilter>();

                    foreach (var sub in subscriptions)
                    {
                        topicFilters.Add(sub);

                        if (topicFilters.Count == Options.MaxTopicFiltersInSubscribeUnsubscribePackets)
                        {
                            await SendSubscribeUnsubscribe(topicFilters, null)
                                .ConfigureAwait(false);
                            topicFilters.Clear();
                        }
                    }

                    await SendSubscribeUnsubscribe(topicFilters, null)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                await HandleSubscriptionExceptionAsync(exception)
                    .ConfigureAwait(false);
            }
        }

        async Task PublishSubscriptionsAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var endTime = DateTime.UtcNow + timeout;

            while (await _subscriptionsQueuedSignal.WaitAsync(GetRemainingTime(endTime), cancellationToken)
                       .ConfigureAwait(false))
            {
                List<MqttTopicFilter> subscriptions;
                HashSet<string> unsubscriptions;

                lock (_subscriptions)
                {
                    subscriptions = _subscriptions.Select(
                            i => new MqttTopicFilter
                            {
                                Topic = i.Key,
                                QualityOfServiceLevel = i.Value
                            })
                        .ToList();

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
                    _reconnectSubscriptions[subscription.Topic] = subscription.QualityOfServiceLevel;
                }

                var addedTopicFilters = new List<MqttTopicFilter>();
                foreach (var subscription in subscriptions)
                {
                    addedTopicFilters.Add(subscription);

                    if (addedTopicFilters.Count == Options.MaxTopicFiltersInSubscribeUnsubscribePackets)
                    {
                        await SendSubscribeUnsubscribe(addedTopicFilters, null)
                            .ConfigureAwait(false);
                        addedTopicFilters.Clear();
                    }
                }

                await SendSubscribeUnsubscribe(addedTopicFilters, null)
                    .ConfigureAwait(false);

                var removedTopicFilters = new List<string>();
                foreach (var unSub in unsubscriptions)
                {
                    removedTopicFilters.Add(unSub);

                    if (removedTopicFilters.Count == Options.MaxTopicFiltersInSubscribeUnsubscribePackets)
                    {
                        await SendSubscribeUnsubscribe(null, removedTopicFilters)
                            .ConfigureAwait(false);
                        removedTopicFilters.Clear();
                    }
                }

                await SendSubscribeUnsubscribe(null, removedTopicFilters)
                    .ConfigureAwait(false);
            }
        }

        async Task<ReconnectionResult> ReconnectIfRequiredAsync(CancellationToken cancellationToken)
        {
            if (InternalClient.IsConnected)
            {
                return ReconnectionResult.StillConnected;
            }

            try
            {
                var result = await InternalClient.ConnectAsync(Options.ClientOptions, cancellationToken)
                    .ConfigureAwait(false);
                return result.IsSessionPresent ? ReconnectionResult.Recovered : ReconnectionResult.Reconnected;
            }
            catch (Exception exception)
            {
                var connectingFailedHandler = ConnectingFailedHandler;
                if (connectingFailedHandler != null)
                {
                    await connectingFailedHandler.HandleConnectingFailedAsync(new ManagedProcessFailedEventArgs(exception))
                        .ConfigureAwait(false);
                }

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

                    await InternalClient.UnsubscribeAsync(unsubscribeOptionsBuilder.Build())
                        .ConfigureAwait(false);
                }

                if (addedSubscriptions != null && addedSubscriptions.Any())
                {
                    var subscribeOptionsBuilder = new MqttClientSubscribeOptionsBuilder();

                    foreach (var addedSubscription in addedSubscriptions)
                    {
                        subscribeOptionsBuilder.WithTopicFilter(addedSubscription);
                    }

                    await InternalClient.SubscribeAsync(subscribeOptionsBuilder.Build())
                        .ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                await HandleSubscriptionExceptionAsync(exception)
                    .ConfigureAwait(false);
            }
        }

        void StartPublishing()
        {
            if (_publishingCancellationToken != null)
            {
                StopPublishing();
            }

            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            _publishingCancellationToken = cancellationTokenSource;

            Task.Run(() => PublishQueuedMessagesAsync(cancellationToken), cancellationToken)
                .RunInBackground(_logger);
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
                var connectionState = await ReconnectIfRequiredAsync(cancellationToken)
                    .ConfigureAwait(false);
                if (connectionState == ReconnectionResult.NotConnected)
                {
                    StopPublishing();
                    await Task.Delay(Options.AutoReconnectDelay, cancellationToken)
                        .ConfigureAwait(false);
                    return;
                }

                if (connectionState == ReconnectionResult.Reconnected)
                {
                    await PublishReconnectSubscriptionsAsync()
                        .ConfigureAwait(false);
                    StartPublishing();
                    return;
                }

                if (connectionState == ReconnectionResult.Recovered)
                {
                    StartPublishing();
                    return;
                }

                if (connectionState == ReconnectionResult.StillConnected)
                {
                    await PublishSubscriptionsAsync(Options.ConnectionCheckInterval, cancellationToken)
                        .ConfigureAwait(false);
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
            try
            {
                await InternalClient.PublishAsync(message.ApplicationMessage)
                    .ConfigureAwait(false);

                using (await _messageQueueLock.WaitAsync(CancellationToken.None)
                           .ConfigureAwait(false)) //lock to avoid conflict with this.PublishAsync
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
                        await _storageManager.RemoveAsync(message)
                            .ConfigureAwait(false);
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
                    using (await _messageQueueLock.WaitAsync(CancellationToken.None)
                               .ConfigureAwait(false)) //lock to avoid conflict with this.PublishAsync
                    {
                        _messageQueue.RemoveFirst(i => i.Id.Equals(message.Id));

                        if (_storageManager != null)
                        {
                            await _storageManager.RemoveAsync(message)
                                .ConfigureAwait(false);
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
                var eventHandler = ApplicationMessageProcessedHandler;
                if (eventHandler != null)
                {
                    var eventArguments = new ApplicationMessageProcessedEventArgs(message, transmitException);
                    await eventHandler.HandleApplicationMessageProcessedAsync(eventArguments)
                        .ConfigureAwait(false);
                }
            }
        }
    }
}