using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Internal;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MqttClientConnectedEventArgs = MQTTnet.Client.MqttClientConnectedEventArgs;
using MqttClientDisconnectedEventArgs = MQTTnet.Client.MqttClientDisconnectedEventArgs;

namespace MQTTnet.Extensions.ManagedClient
{
    public class ManagedMqttClient : IManagedMqttClient
    {
        private readonly BlockingQueue<ManagedMqttApplicationMessage> _messageQueue = new BlockingQueue<ManagedMqttApplicationMessage>();
        private readonly Dictionary<string, MqttQualityOfServiceLevel> _subscriptions = new Dictionary<string, MqttQualityOfServiceLevel>();
        private readonly HashSet<string> _unsubscriptions = new HashSet<string>();

        private readonly IMqttClient _mqttClient;
        private readonly IMqttNetChildLogger _logger;

        private CancellationTokenSource _connectionCancellationToken;
        private CancellationTokenSource _publishingCancellationToken;

        private ManagedMqttClientStorageManager _storageManager;
        
        private bool _subscriptionsNotPushed;

        public ManagedMqttClient(IMqttClient mqttClient, IMqttNetChildLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            _mqttClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));

            _mqttClient.Connected += OnConnected;
            _mqttClient.Disconnected += OnDisconnected;
            _mqttClient.ApplicationMessageReceived += OnApplicationMessageReceived;

            _logger = logger.CreateChildLogger(nameof(ManagedMqttClient));
        }

        public bool IsConnected => _mqttClient.IsConnected;
        public bool IsStarted => _connectionCancellationToken != null;
        public int PendingApplicationMessagesCount => _messageQueue.Count;
        public IManagedMqttClientOptions Options { get; private set; }

        public event EventHandler<MqttClientConnectedEventArgs> Connected;
        public event EventHandler<MqttClientDisconnectedEventArgs> Disconnected;

        public event EventHandler<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;
        public event EventHandler<ApplicationMessageProcessedEventArgs> ApplicationMessageProcessed;
        public event EventHandler<ApplicationMessageSkippedEventArgs> ApplicationMessageSkipped;

        public event EventHandler<MqttManagedProcessFailedEventArgs> ConnectingFailed;
        public event EventHandler<MqttManagedProcessFailedEventArgs> SynchronizingSubscriptionsFailed;

        public async Task StartAsync(IManagedMqttClientOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (options.ClientOptions == null) throw new ArgumentException("The client options are not set.", nameof(options));

            if (!options.ClientOptions.CleanSession)
            {
                throw new NotSupportedException("The managed client does not support existing sessions.");
            }

            if (_connectionCancellationToken != null) throw new InvalidOperationException("The managed client is already started.");

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

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(() => MaintainConnectionAsync(_connectionCancellationToken.Token), _connectionCancellationToken.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            _logger.Info("Started");
        }

        public Task StopAsync()
        {
            StopPublishing();
            StopMaintainingConnection();

            _messageQueue.Clear();

            return Task.FromResult(0);
        }

        public Task PublishAsync(MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            return PublishAsync(new ManagedMqttApplicationMessageBuilder().WithApplicationMessage(applicationMessage).Build());
        }

        public async Task PublishAsync(ManagedMqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            ManagedMqttApplicationMessage removedMessage = null;
            lock (_messageQueue)
            {
                if (_messageQueue.Count >= Options.MaxPendingMessages)
                {
                    if (Options.PendingMessagesOverflowStrategy == MqttPendingMessagesOverflowStrategy.DropNewMessage)
                    {
                        _logger.Verbose("Skipping publish of new application message because internal queue is full.");
                        ApplicationMessageSkipped?.Invoke(this, new ApplicationMessageSkippedEventArgs(applicationMessage));
                        return;
                    }

                    if (Options.PendingMessagesOverflowStrategy == MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage)
                    {
                        removedMessage = _messageQueue.RemoveFirst();
                        _logger.Verbose("Removed oldest application message from internal queue because it is full.");
                        ApplicationMessageSkipped?.Invoke(this, new ApplicationMessageSkippedEventArgs(removedMessage));
                    }
                }

                _messageQueue.Enqueue(applicationMessage);
            }

            if (_storageManager != null)
            {
                if (removedMessage != null)
                {
                    await _storageManager.RemoveAsync(removedMessage).ConfigureAwait(false);
                }

                await _storageManager.AddAsync(applicationMessage).ConfigureAwait(false);
            }
        }

        public Task SubscribeAsync(IEnumerable<TopicFilter> topicFilters)
        {
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            lock (_subscriptions)
            {
                foreach (var topicFilter in topicFilters)
                {
                    _subscriptions[topicFilter.Topic] = topicFilter.QualityOfServiceLevel;
                    _subscriptionsNotPushed = true;
                }
            }

            return Task.FromResult(0);
        }

        public Task UnsubscribeAsync(IEnumerable<string> topics)
        {
            if (topics == null) throw new ArgumentNullException(nameof(topics));

            lock (_subscriptions)
            {
                foreach (var topic in topics)
                {
                    if (_subscriptions.Remove(topic))
                    {
                        _unsubscriptions.Add(topic);
                        _subscriptionsNotPushed = true;
                    }
                }
            }

            return Task.FromResult(0);
        }

        public void Dispose()
        {
            _connectionCancellationToken?.Dispose();
            _publishingCancellationToken?.Dispose();
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
                _logger.Error(exception, "Unhandled exception while maintaining connection.");
            }
            finally
            {
                await _mqttClient.DisconnectAsync().ConfigureAwait(false);
                _logger.Info("Stopped");
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

                if (connectionState == ReconnectionResult.Reconnected || _subscriptionsNotPushed)
                {
                    await SynchronizeSubscriptionsAsync().ConfigureAwait(false);
                    StartPublishing();
                    return;
                }

                if (connectionState == ReconnectionResult.StillConnected)
                {
                    await Task.Delay(Options.ConnectionCheckInterval, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (MqttCommunicationException exception)
            {
                _logger.Warning(exception, "Communication exception while maintaining connection.");
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Unhandled exception while maintaining connection.");
            }
        }

        private void PublishQueuedMessages(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && _mqttClient.IsConnected)
                {
                    //Peek at the message without dequeueing in order to prevent the
                    //possibility of the queue growing beyond the configured cap.
                    //Previously, messages could be re-enqueued if there was an
                    //exception, and this re-enqueueing did not honor the cap.
                    //Furthermore, because re-enqueueing would shuffle the order
                    //of the messages, the DropOldestQueuedMessage strategy would
                    //be unable to know which message is actually the oldest and would
                    //instead drop the first item in the queue.
                    var message = _messageQueue.PeekAndWait();
                    if (message == null)
                    {
                        continue;
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    TryPublishQueuedMessage(message);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Unhandled exception while publishing queued application messages.");
            }
            finally
            {
                _logger.Verbose("Stopped publishing messages.");
            }
        }

        private void TryPublishQueuedMessage(ManagedMqttApplicationMessage message)
        {
            Exception transmitException = null;
            try
            {
                _mqttClient.PublishAsync(message.ApplicationMessage).GetAwaiter().GetResult();
                lock (_messageQueue) //lock to avoid conflict with this.PublishAsync
                {
                    //While publishing this message, this.PublishAsync could have booted this
                    //message off the queue to make room for another (when using a cap
                    //with the DropOldestQueuedMessage strategy).  If the first item
                    //in the queue is equal to this message, then it's safe to remove
                    //it from the queue.  If not, that means this.PublishAsync has already
                    //removed it, in which case we don't want to do anything.
                    _messageQueue.RemoveFirst(i => i.Id.Equals(message.Id));
                }
                _storageManager?.RemoveAsync(message).GetAwaiter().GetResult();
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
                    lock (_messageQueue) //lock to avoid conflict with this.PublishAsync
                    {
                        _messageQueue.RemoveFirst(i => i.Id.Equals(message.Id));
                    }
                }
            }
            catch (Exception exception)
            {
                transmitException = exception;
                _logger.Error(exception, $"Unhandled exception while publishing application message ({message.Id}).");
            }
            finally
            {
                ApplicationMessageProcessed?.Invoke(this, new ApplicationMessageProcessedEventArgs(message, transmitException));
            }
        }

        private async Task SynchronizeSubscriptionsAsync()
        {
            _logger.Info(nameof(ManagedMqttClient), "Synchronizing subscriptions");

            List<TopicFilter> subscriptions;
            HashSet<string> unsubscriptions;

            lock (_subscriptions)
            {
                subscriptions = _subscriptions.Select(i => new TopicFilter(i.Key, i.Value)).ToList();

                unsubscriptions = new HashSet<string>(_unsubscriptions);
                _unsubscriptions.Clear();

                _subscriptionsNotPushed = false;
            }

            if (!subscriptions.Any() && !unsubscriptions.Any())
            {
                return;
            }

            try
            {
                if (unsubscriptions.Any())
                {
                    await _mqttClient.UnsubscribeAsync(unsubscriptions).ConfigureAwait(false);
                }

                if (subscriptions.Any())
                {
                    await _mqttClient.SubscribeAsync(subscriptions).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                _logger.Warning(exception, "Synchronizing subscriptions failed.");
                _subscriptionsNotPushed = true;

                SynchronizingSubscriptionsFailed?.Invoke(this, new MqttManagedProcessFailedEventArgs(exception));
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
                await _mqttClient.ConnectAsync(Options.ClientOptions).ConfigureAwait(false);
                return ReconnectionResult.Reconnected;
            }
            catch (Exception exception)
            {
                ConnectingFailed?.Invoke(this, new MqttManagedProcessFailedEventArgs(exception));
                return ReconnectionResult.NotConnected;
            }
        }

        private void OnApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            ApplicationMessageReceived?.Invoke(this, eventArgs);
        }

        private void OnDisconnected(object sender, MqttClientDisconnectedEventArgs eventArgs)
        {
            Disconnected?.Invoke(this, eventArgs);
        }

        private void OnConnected(object sender, MqttClientConnectedEventArgs eventArgs)
        {
            Connected?.Invoke(this, eventArgs);
        }

        private void StartPublishing()
        {
            if (_publishingCancellationToken != null)
            {
                StopPublishing();
            }

            var cts = new CancellationTokenSource();
            _publishingCancellationToken = cts;

            Task.Factory.StartNew(() => PublishQueuedMessages(cts.Token), cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
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
    }
}
