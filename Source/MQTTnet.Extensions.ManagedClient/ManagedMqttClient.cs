using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Receiving;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Internal;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace MQTTnet.Extensions.ManagedClient
{
    public class ManagedMqttClient : IManagedMqttClient
    {
        private readonly BlockingQueue<ManagedMqttApplicationMessage> _messageQueue = new BlockingQueue<ManagedMqttApplicationMessage>();

        /// <summary>
        /// The subscriptions are managed in 2 separate buckets:
        /// <see cref="_subscriptions"/> and <see cref="_unsubscriptions"/> are processed during normal operation
        /// and are moved to the <see cref="_reconnectSubscriptions"/> when they get processed. They can be accessed by
        /// any thread and are therefore mutex'ed. <see cref="_reconnectSubscriptions"/> get sent to the broker
        ///  at reconnect and are solely owned by <see cref="MaintainConnectionAsync"/>.
        /// </summary>
        private readonly Dictionary<string, MqttQualityOfServiceLevel> _reconnectSubscriptions = new Dictionary<string, MqttQualityOfServiceLevel>();
        private readonly Dictionary<string, MqttQualityOfServiceLevel> _subscriptions = new Dictionary<string, MqttQualityOfServiceLevel>();
        private readonly HashSet<string> _unsubscriptions = new HashSet<string>();
        private readonly SemaphoreSlim _subscriptionsQueuedSignal = new SemaphoreSlim(0);

        private readonly IMqttClient _mqttClient;
        private readonly IMqttNetChildLogger _logger;

        private readonly AsyncLock _messageQueueLock = new AsyncLock();

        private CancellationTokenSource _connectionCancellationToken;
        private CancellationTokenSource _publishingCancellationToken;
        private Task _maintainConnectionTask;

        private ManagedMqttClientStorageManager _storageManager;

        private bool _disposed;

        public ManagedMqttClient(IMqttClient mqttClient, IMqttNetChildLogger logger)
        {
            _mqttClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));

            if (logger == null) throw new ArgumentNullException(nameof(logger));
            _logger = logger.CreateChildLogger(nameof(ManagedMqttClient));
        }

        public bool IsConnected => _mqttClient.IsConnected;
        public bool IsStarted => _connectionCancellationToken != null;
        public int PendingApplicationMessagesCount => _messageQueue.Count;
        public IManagedMqttClientOptions Options { get; private set; }

        public IMqttClientConnectedHandler ConnectedHandler
        {
            get => _mqttClient.ConnectedHandler;
            set => _mqttClient.ConnectedHandler = value;
        }

        public IMqttClientDisconnectedHandler DisconnectedHandler
        {
            get => _mqttClient.DisconnectedHandler;
            set => _mqttClient.DisconnectedHandler = value;
        }

        public IMqttApplicationMessageReceivedHandler ApplicationMessageReceivedHandler
        {
            get => _mqttClient.ApplicationMessageReceivedHandler;
            set => _mqttClient.ApplicationMessageReceivedHandler = value;
        }

        public IApplicationMessageProcessedHandler ApplicationMessageProcessedHandler { get; set; }

        public IApplicationMessageSkippedHandler ApplicationMessageSkippedHandler { get; set; }

        public IConnectingFailedHandler ConnectingFailedHandler { get; set; }

        public ISynchronizingSubscriptionsFailedHandler SynchronizingSubscriptionsFailedHandler { get; set; }

        public async Task StartAsync(IManagedMqttClientOptions options)
        {
            ThrowIfDisposed();

            if (options == null) throw new ArgumentNullException(nameof(options));
            if (options.ClientOptions == null) throw new ArgumentException("The client options are not set.", nameof(options));


            if (!_maintainConnectionTask?.IsCompleted ?? false) throw new InvalidOperationException("The managed client is already started.");

            Options = options;

            if (Options.Storage != null)
            {
                _storageManager = new ManagedMqttClientStorageManager(Options.Storage);
                var messages = await _storageManager.LoadQueuedMessagesAsync().ConfigureAwait(false);

                foreach (var message in messages)
                {
                    _messageQueue.Enqueue(message);
                }
            }

            _connectionCancellationToken = new CancellationTokenSource();

            _maintainConnectionTask = Task.Run(() => MaintainConnectionAsync(_connectionCancellationToken.Token), _connectionCancellationToken.Token);
            _maintainConnectionTask.Forget(_logger);

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

        public async Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            await PublishAsync(new ManagedMqttApplicationMessageBuilder().WithApplicationMessage(applicationMessage).Build()).ConfigureAwait(false);
            return new MqttClientPublishResult();
        }

        public async Task PublishAsync(ManagedMqttApplicationMessage applicationMessage)
        {
            ThrowIfDisposed();

            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            MqttTopicValidator.ThrowIfInvalid(applicationMessage.ApplicationMessage.Topic);

            ManagedMqttApplicationMessage removedMessage = null;
            ApplicationMessageSkippedEventArgs applicationMessageSkippedEventArgs = null;

            try
            {
                using (await _messageQueueLock.WaitAsync(CancellationToken.None).ConfigureAwait(false))
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
                if (applicationMessageSkippedEventArgs != null)
                {
                    var applicationMessageSkippedHandler = ApplicationMessageSkippedHandler;
                    if (applicationMessageSkippedHandler != null)
                    {
                        await applicationMessageSkippedHandler.HandleApplicationMessageSkippedAsync(applicationMessageSkippedEventArgs).ConfigureAwait(false);
                    }
                }

            }
        }

        public Task SubscribeAsync(IEnumerable<TopicFilter> topicFilters)
        {
            ThrowIfDisposed();

            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

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

        public Task UnsubscribeAsync(IEnumerable<string> topics)
        {
            ThrowIfDisposed();

            if (topics == null) throw new ArgumentNullException(nameof(topics));

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

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            StopPublishing();
            StopMaintainingConnection();

            if (_maintainConnectionTask != null)
            {
                Task.WaitAny(_maintainConnectionTask);
                _maintainConnectionTask = null;
            }

            _messageQueue.Dispose();
            _messageQueueLock.Dispose();
            _mqttClient.Dispose();
            _subscriptionsQueuedSignal.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ManagedMqttClient));
            }
        }

        private async Task MaintainConnectionAsync(CancellationToken cancellationToken)
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
                if (!_disposed)
                {
                    try
                    {
                        await _mqttClient.DisconnectAsync().ConfigureAwait(false);
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

        private async Task TryMaintainConnectionAsync(CancellationToken cancellationToken)
        {
            try
            {
                var connectionState = await ReconnectIfRequiredAsync().ConfigureAwait(false);
                if (connectionState == ReconnectionResult.NotConnected)
                {
                    StopPublishing();
                    await Task.Delay(Options.AutoReconnectDelay, cancellationToken).ConfigureAwait(false);
                    return;
                }

                if (connectionState == ReconnectionResult.Reconnected)
                {
                    await PublishReconnectSubscriptionsAsync().ConfigureAwait(false);
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
                    await PublishSubscriptionsAsync(Options.ConnectionCheckInterval, cancellationToken).ConfigureAwait(false);
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

        private async Task PublishQueuedMessagesAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && _mqttClient.IsConnected)
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

        private async Task TryPublishQueuedMessageAsync(ManagedMqttApplicationMessage message)
        {
            Exception transmitException = null;
            try
            {
                await _mqttClient.PublishAsync(message.ApplicationMessage).ConfigureAwait(false);

                using (await _messageQueueLock.WaitAsync(CancellationToken.None).ConfigureAwait(false)) //lock to avoid conflict with this.PublishAsync
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

                _logger.Warning(exception, $"Publishing application ({message.Id}) message failed.");

                if (message.ApplicationMessage.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtMostOnce)
                {
                    //If QoS 0, we don't want this message to stay on the queue.
                    //If QoS 1 or 2, it's possible that, when using a cap, this message
                    //has been booted off the queue by this.PublishAsync, in which case this
                    //thread will not continue to try to publish it. While this does
                    //contradict the expected behavior of QoS 1 and 2, that's also true
                    //for the usage of a message queue cap, so it's still consistent
                    //with prior behavior in that way.
                    using (await _messageQueueLock.WaitAsync(CancellationToken.None).ConfigureAwait(false)) //lock to avoid conflict with this.PublishAsync
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
                _logger.Error(exception, $"Error while publishing application message ({message.Id}).");
            }
            finally
            {
                var eventHandler = ApplicationMessageProcessedHandler;
                if (eventHandler != null)
                {
                    var eventArguments = new ApplicationMessageProcessedEventArgs(message, transmitException);
                    await eventHandler.HandleApplicationMessageProcessedAsync(eventArguments).ConfigureAwait(false);
                }
            }
        }

        private async Task PublishSubscriptionsAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            var endTime = DateTime.UtcNow + timeout;
            while (await _subscriptionsQueuedSignal.WaitAsync(GetRemainingTime(endTime), cancellationToken).ConfigureAwait(false))
            {
                List<TopicFilter> subscriptions;
                HashSet<string> unsubscriptions;

                lock (_subscriptions)
                {
                    subscriptions = _subscriptions.Select(i => new TopicFilter { Topic = i.Key, QualityOfServiceLevel = i.Value }).ToList();
                    _subscriptions.Clear();
                    unsubscriptions = new HashSet<string>(_unsubscriptions);
                    _unsubscriptions.Clear();
                }

                if (!subscriptions.Any() && !unsubscriptions.Any())
                {
                    continue;
                }

                _logger.Verbose($"Publishing subscriptions ({subscriptions.Count} subscriptions and {unsubscriptions.Count} unsubscriptions)");

                foreach (var unsubscription in unsubscriptions)
                {
                    _reconnectSubscriptions.Remove(unsubscription);
                }

                foreach (var subscription in subscriptions)
                {
                    _reconnectSubscriptions[subscription.Topic] = subscription.QualityOfServiceLevel;
                }

                try
                {
                    if (unsubscriptions.Any())
                    {
                        await _mqttClient.UnsubscribeAsync(unsubscriptions.ToArray()).ConfigureAwait(false);
                    }

                    if (subscriptions.Any())
                    {
                        await _mqttClient.SubscribeAsync(subscriptions.ToArray()).ConfigureAwait(false);
                    }
                }
                catch (Exception exception)
                {
                    await HandleSubscriptionExceptionAsync(exception).ConfigureAwait(false);
                }
            }
        }

        private async Task PublishReconnectSubscriptionsAsync()
        {
            _logger.Info("Publishing subscriptions at reconnect");

            try
            {
                if (_reconnectSubscriptions.Any())
                {
                    var subscriptions = _reconnectSubscriptions.Select(i => new TopicFilter { Topic = i.Key, QualityOfServiceLevel = i.Value });
                    await _mqttClient.SubscribeAsync(subscriptions.ToArray()).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                await HandleSubscriptionExceptionAsync(exception).ConfigureAwait(false);
            }
        }

        private async Task HandleSubscriptionExceptionAsync(Exception exception)
        {
            _logger.Warning(exception, "Synchronizing subscriptions failed.");

            var synchronizingSubscriptionsFailedHandler = SynchronizingSubscriptionsFailedHandler;
            if (SynchronizingSubscriptionsFailedHandler != null)
            {
                await synchronizingSubscriptionsFailedHandler.HandleSynchronizingSubscriptionsFailedAsync(new ManagedProcessFailedEventArgs(exception)).ConfigureAwait(false);
            }
        }

        private async Task<ReconnectionResult> ReconnectIfRequiredAsync()
        {
            if (_mqttClient.IsConnected)
            {
                return ReconnectionResult.StillConnected;
            }

            try
            {
                var result = await _mqttClient.ConnectAsync(Options.ClientOptions).ConfigureAwait(false);
                return result.IsSessionPresent ? ReconnectionResult.Recovered : ReconnectionResult.Reconnected;
            }
            catch (Exception exception)
            {
                var connectingFailedHandler = ConnectingFailedHandler;
                if (connectingFailedHandler != null)
                {
                    await connectingFailedHandler.HandleConnectingFailedAsync(new ManagedProcessFailedEventArgs(exception)).ConfigureAwait(false);
                }

                return ReconnectionResult.NotConnected;
            }
        }

        private void StartPublishing()
        {
            if (_publishingCancellationToken != null)
            {
                StopPublishing();
            }

            var cts = new CancellationTokenSource();
            _publishingCancellationToken = cts;

            Task.Run(() => PublishQueuedMessagesAsync(cts.Token), cts.Token).Forget(_logger);
        }

        private void StopPublishing()
        {
            _publishingCancellationToken?.Cancel(false);
            _publishingCancellationToken?.Dispose();
            _publishingCancellationToken = null;
        }

        private void StopMaintainingConnection()
        {
            _connectionCancellationToken?.Cancel(false);
            _connectionCancellationToken?.Dispose();
            _connectionCancellationToken = null;
        }

        private TimeSpan GetRemainingTime(DateTime endTime)
        {
            var remainingTime = endTime - DateTime.UtcNow;
            return remainingTime < TimeSpan.Zero ? TimeSpan.Zero : remainingTime;
        }
    }
}
