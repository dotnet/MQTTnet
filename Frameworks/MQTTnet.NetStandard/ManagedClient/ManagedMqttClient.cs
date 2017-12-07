using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Protocol;

namespace MQTTnet.ManagedClient
{
    public class ManagedMqttClient : IManagedMqttClient, IDisposable
    {
        private readonly BlockingCollection<MqttApplicationMessage> _messageQueue = new BlockingCollection<MqttApplicationMessage>();
        private readonly Dictionary<string, MqttQualityOfServiceLevel> _subscriptions = new Dictionary<string, MqttQualityOfServiceLevel>();
        private readonly SemaphoreSlim _subscriptionsSemaphore = new SemaphoreSlim(1, 1);

        private readonly IMqttClient _mqttClient;
        private readonly IMqttNetLogger _logger;

        private CancellationTokenSource _connectionCancellationToken;
        private CancellationTokenSource _publishingCancellationToken;

        private ManagedMqttClientStorageManager _storageManager;
        private IManagedMqttClientOptions _options;

        private bool _subscriptionsNotPushed;
        
        public ManagedMqttClient(IMqttClient mqttClient, IMqttNetLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mqttClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));

            _mqttClient.Connected += OnConnected;
            _mqttClient.Disconnected += OnDisconnected;
            _mqttClient.ApplicationMessageReceived += OnApplicationMessageReceived;
        }

        public bool IsConnected => _mqttClient.IsConnected;

        public event EventHandler<MqttClientConnectedEventArgs> Connected;
        public event EventHandler<MqttClientDisconnectedEventArgs> Disconnected;
        public event EventHandler<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;

        public async Task StartAsync(IManagedMqttClientOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (options.ClientOptions == null) throw new ArgumentException("The client options are not set.", nameof(options));

            if (!options.ClientOptions.CleanSession)
            {
                throw new NotSupportedException("The managed client does not support existing sessions.");
            }

            if (_connectionCancellationToken != null) throw new InvalidOperationException("The managed client is already started.");

            _options = options;
            
            if (_options.Storage != null)
            {
                _storageManager = new ManagedMqttClientStorageManager(_options.Storage);
                await _storageManager.LoadQueuedMessagesAsync().ConfigureAwait(false);
            }

            _connectionCancellationToken = new CancellationTokenSource();
            
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(async () => await MaintainConnectionAsync(_connectionCancellationToken.Token), _connectionCancellationToken.Token).ConfigureAwait(false);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            _logger.Info<ManagedMqttClient>("Started");
        }

        public Task StopAsync()
        {
            StopPublishing();
            StopMaintainingConnection();

            while (_messageQueue.Any())
            {
                _messageQueue.Take();
            }

            return Task.FromResult(0);
        }
        
        public async Task PublishAsync(IEnumerable<MqttApplicationMessage> applicationMessages)
        {
            if (applicationMessages == null) throw new ArgumentNullException(nameof(applicationMessages));

            foreach (var applicationMessage in applicationMessages)
            {
                if (_storageManager != null)
                {
                    await _storageManager.AddAsync(applicationMessage).ConfigureAwait(false);
                }
                
                _messageQueue.Add(applicationMessage);
            }
        }

        public async Task SubscribeAsync(IEnumerable<TopicFilter> topicFilters)
        {
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            await _subscriptionsSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                foreach (var topicFilter in topicFilters)
                {
                    _subscriptions[topicFilter.Topic] = topicFilter.QualityOfServiceLevel;
                    _subscriptionsNotPushed = true;
                }
            }
            finally
            {
                _subscriptionsSemaphore.Release();
            }
        }

        public async Task UnsubscribeAsync(IEnumerable<string> topics)
        {
            await _subscriptionsSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                foreach (var topic in topics)
                {
                    if (_subscriptions.Remove(topic))
                    {
                        _subscriptionsNotPushed = true;
                    }
                }
            }
            finally
            {
                _subscriptionsSemaphore.Release();
            }
        }

        public void Dispose()
        {
            _messageQueue?.Dispose();
            _subscriptionsSemaphore?.Dispose();
            _connectionCancellationToken?.Dispose();
            _publishingCancellationToken?.Dispose();
        }

        private async Task MaintainConnectionAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await TryMaintainConnectionAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.Error<ManagedMqttClient>(exception, "Unhandled exception while maintaining connection.");
            }
            finally
            {
                await _mqttClient.DisconnectAsync().ConfigureAwait(false);
                _logger.Info<ManagedMqttClient>("Stopped");
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
                    await Task.Delay(_options.AutoReconnectDelay, cancellationToken).ConfigureAwait(false);
                    return;
                }

                if (connectionState == ReconnectionResult.Reconnected || _subscriptionsNotPushed)
                {
                    await PushSubscriptionsAsync().ConfigureAwait(false);

                    StartPublishing();

                    
                    return;
                }

                if (connectionState == ReconnectionResult.StillConnected)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (MqttCommunicationException exception)
            {
                _logger.Warning<ManagedMqttClient>(exception, "Communication exception while maintaining connection.");
            }
            catch (Exception exception)
            {
                _logger.Error<ManagedMqttClient>(exception, "Unhandled exception while maintaining connection.");
            }
        }
        
        private async Task PublishQueuedMessagesAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var message = _messageQueue.Take(cancellationToken);
                    if (message == null)
                    {
                        continue;
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        continue;
                    }

                    await TryPublishQueuedMessageAsync(message).ConfigureAwait(false);                 
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.Error<ManagedMqttClient>(exception, "Unhandled exception while publishing queued application messages.");
            }
            finally
            {
                _logger.Trace<ManagedMqttClient>("Stopped publishing messages.");
            }
        }

        private async Task TryPublishQueuedMessageAsync(MqttApplicationMessage message)
        {
            try
            {
                await _mqttClient.PublishAsync(message).ConfigureAwait(false);

                if (_storageManager != null)
                {
                    await _storageManager.RemoveAsync(message).ConfigureAwait(false);
                }
            }
            catch (MqttCommunicationException exception)
            {
                _logger.Warning<ManagedMqttClient>(exception, "Publishing application message failed.");

                if (message.QualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce)
                {
                    _messageQueue.Add(message);
                }
            }
            catch (Exception exception)
            {
                _logger.Error<ManagedMqttClient>(exception, "Unhandled exception while publishing queued application message.");
            }
        }

        private async Task PushSubscriptionsAsync()
        {
            _logger.Info<ManagedMqttClient>(nameof(ManagedMqttClient), "Synchronizing subscriptions");

            List<TopicFilter> subscriptions;
            await _subscriptionsSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                subscriptions = _subscriptions.Select(i => new TopicFilter(i.Key, i.Value)).ToList();
                _subscriptionsNotPushed = false;
            }
            finally
            {
                _subscriptionsSemaphore.Release();
            }

            if (!subscriptions.Any())
            {
                return;
            }

            try
            {
                await _mqttClient.SubscribeAsync(subscriptions).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.Warning<ManagedMqttClient>(exception, "Synchronizing subscriptions failed");
                _subscriptionsNotPushed = true;
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
                await _mqttClient.ConnectAsync(_options.ClientOptions).ConfigureAwait(false);
                return ReconnectionResult.Reconnected;
            }
            catch (Exception)
            {
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

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(async () => await PublishQueuedMessagesAsync(cts.Token).ConfigureAwait(false), cts.Token).ConfigureAwait(false);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
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
