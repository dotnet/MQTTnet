// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Implementations;
using MQTTnet.Internal;

namespace MQTTnet.Extensions.ManagedClient
{
    public sealed class NewManagedMqttClient : Disposable, IManagedMqttClient
    {
        readonly AsyncEvent<ApplicationMessageProcessedEventArgs> _applicationMessageProcessedEvent = new AsyncEvent<ApplicationMessageProcessedEventArgs>();
        readonly ApplicationMessagesManager _applicationMessagesManager;
        readonly MqttConnectionManager _connectionManager;
        readonly AsyncEvent<EventArgs> _isConnectedChangedEvent = new AsyncEvent<EventArgs>();
        readonly MqttNetSourceLogger _logger;
        readonly ManagedMqttSubscriptionsManager _subscriptionsManager;

        readonly ManualResetEventSlim _workSignal = new ManualResetEventSlim();

        CancellationTokenSource _workerCancellationToken;
        Task _workerTask;

        public NewManagedMqttClient(IMqttClient mqttClient, IMqttNetLogger logger)
        {
            InternalClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            InternalClient.DisconnectedAsync += OnInternalClientDisconnected;

            _logger = logger.WithSource(nameof(NewManagedMqttClient));

            _connectionManager = new MqttConnectionManager(mqttClient, logger);
            _subscriptionsManager = new ManagedMqttSubscriptionsManager(mqttClient, logger);
            _applicationMessagesManager = new ApplicationMessagesManager(mqttClient, logger);

            // TODO: Start timer for periodical connection checking.
        }

        public event Func<EventArgs, Task> ApplicationMessageEnqueueingAsync
        {
            add => _applicationMessagesManager.ApplicationMessageEnqueueingEvent.AddHandler(value);
            remove => _applicationMessagesManager.ApplicationMessageEnqueueingEvent.RemoveHandler(value);
        }

        public event Func<ApplicationMessageProcessedEventArgs, Task> ApplicationMessageProcessedAsync
        {
            add => _applicationMessageProcessedEvent.AddHandler(value);
            remove => _applicationMessageProcessedEvent.RemoveHandler(value);
        }

        public event Func<MqttApplicationMessageReceivedEventArgs, Task> ApplicationMessageReceivedAsync
        {
            add => InternalClient.ApplicationMessageReceivedAsync += value;
            remove => InternalClient.ApplicationMessageReceivedAsync += value;
        }

        public event Func<ApplicationMessageDroppedEventArgs, Task> ApplicationMessageDroppedAsync
        {
            add => _applicationMessagesManager.ApplicationMessageDroppedEvent.AddHandler(value);
            remove => _applicationMessagesManager.ApplicationMessageDroppedEvent.RemoveHandler(value);
        }

        public event Func<MqttClientConnectedEventArgs, Task> ConnectedAsync
        {
            add => _connectionManager.ConnectedEvent.AddHandler(value);
            remove => _connectionManager.ConnectedEvent.RemoveHandler(value);
        }

        public event Func<ConnectingFailedEventArgs, Task> ConnectingFailedAsync
        {
            add => _connectionManager.ConnectingFailedEvent.AddHandler(value);
            remove => _connectionManager.ConnectingFailedEvent.RemoveHandler(value);
        }

        public event Func<MqttClientDisconnectedEventArgs, Task> DisconnectedAsync
        {
            add => _connectionManager.DisconnectedEvent.AddHandler(value);
            remove => _connectionManager.DisconnectedEvent.RemoveHandler(value);
        }

        public event Func<SubscribeProcessedEventArgs, Task> SubscribeProcessedAsync
        {
            add => _subscriptionsManager.SubscribeProcessedEvent.AddHandler(value);
            remove => _subscriptionsManager.SubscribeProcessedEvent.RemoveHandler(value);
        }

        public event Func<UnsubscribeProcessedEventArgs, Task> UnsubscribeProcessedAsync
        {
            add => _subscriptionsManager.UnsubscribeProcessedEvent.AddHandler(value);
            remove => _subscriptionsManager.UnsubscribeProcessedEvent.RemoveHandler(value);
        }

        public IMqttClient InternalClient { get; }

        public bool IsConnected { get; private set; }

        public bool IsStarted { get; private set; }

        public ManagedMqttClientOptions Options { get; private set; }

        public int EnqueuedApplicationMessagesCount => _applicationMessagesManager.EnqueuedApplicationMessagesCount;

        public Task EnqueueAsync(MqttApplicationMessage applicationMessage)
        {
            return EnqueueAsync(new ManagedMqttApplicationMessage(Guid.NewGuid().ToString(), applicationMessage));
        }

        public async Task EnqueueAsync(ManagedMqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null)
            {
                throw new ArgumentNullException(nameof(applicationMessage));
            }

            await _applicationMessagesManager.Enqueue(applicationMessage).ConfigureAwait(false);
            _workSignal.Set();
        }

        public Task PingAsync(CancellationToken cancellationToken = default)
        {
            return InternalClient.PingAsync(cancellationToken);
        }

        public Task StartAsync(ManagedMqttClientOptions options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));

            _workerCancellationToken = new CancellationTokenSource();

            _workerTask = Task.Factory.StartNew(
                () => WorkerLoop(_workerCancellationToken.Token),
                _workerCancellationToken.Token,
                TaskCreationOptions.PreferFairness,
                TaskScheduler.Default);

            _workSignal.Set();

            return PlatformAbstractionLayer.CompletedTask;
        }

        public async Task StopAsync(MqttClientDisconnectOptions options, CancellationToken cancellationToken = default)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            // TODO: Send proper Disconnect signal.

            try
            {
                await InternalClient.DisconnectAsync(options, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Error while disconnecting.");
            }

            _workerCancellationToken?.Cancel(false);
            _workerCancellationToken?.Dispose();

            _workerCancellationToken = null;

            if (_workerTask != null)
            {
            }
        }

        public Task SubscribeAsync(MqttClientSubscribeOptions options)
        {
            ThrowIfDisposed();

            _subscriptionsManager.Subscribe(options);
            _workSignal.Set();

            return PlatformAbstractionLayer.CompletedTask;
        }

        public Task UnsubscribeAsync(MqttClientUnsubscribeOptions options)
        {
            ThrowIfDisposed();

            _subscriptionsManager.Unsubscribe(options);
            _workSignal.Set();

            return PlatformAbstractionLayer.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
            }
        }

        Task OnInternalClientDisconnected(MqttClientDisconnectedEventArgs eventArgs)
        {
            _workSignal.Set();
            return PlatformAbstractionLayer.CompletedTask;
        }

        async Task UpdateConnectionStatus(MaintainConnectionResult maintainConnectionResult)
        {
            var isConnected = maintainConnectionResult != MaintainConnectionResult.NotConnected;

            if (IsConnected.Equals(isConnected))
            {
                return;
            }

            IsConnected = isConnected;

            if (_isConnectedChangedEvent.HasHandlers)
            {
                await _isConnectedChangedEvent.InvokeAsync(EventArgs.Empty).ConfigureAwait(false);
            }
        }

        async Task WorkerLoop(CancellationToken cancellationToken)
        {
            try
            {
                IsStarted = true;

                while (!cancellationToken.IsCancellationRequested)
                {
                    _workSignal.Wait(cancellationToken);
                    _workSignal.Reset();

                    var maintainConnectionResult = await _connectionManager.MaintainConnection(Options, cancellationToken).ConfigureAwait(false);
                    await UpdateConnectionStatus(maintainConnectionResult).ConfigureAwait(false);

                    if (maintainConnectionResult == MaintainConnectionResult.ConnectedEstablished && Options.ClientOptions.CleanSession)
                    {
                        // Since the session is clean the server will now know any subscription so that
                        // we have to subscribe to all of them again.
                        _subscriptionsManager.ResubscribeAll();
                    }

                    if (IsConnected)
                    {
                        await _subscriptionsManager.Synchronize(cancellationToken).ConfigureAwait(false);
                    }

                    if (IsConnected)
                    {
                        // TODO: Move into pending messages manager + locking.
                        while (_applicationMessagesManager.TryPeek(out var nextPendingMessage))
                        {
                            Exception processingException = null;
                            try
                            {
                                _logger.Verbose("Publishing managed application message {0}.", nextPendingMessage.Id);

                                await InternalClient.PublishAsync(nextPendingMessage.ApplicationMessage, cancellationToken).ConfigureAwait(false);

                                // The message was delivered successfully. So we can get rid of it.
                                _applicationMessagesManager.Dequeue();
                            }
                            catch (Exception exception)
                            {
                                processingException = exception;
                            }
                            finally
                            {
                                if (_applicationMessageProcessedEvent.HasHandlers)
                                {
                                    var eventArgs = new ApplicationMessageProcessedEventArgs(nextPendingMessage, processingException);
                                    await _applicationMessageProcessedEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
                                }
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
#if !NETSTANDARD1_3
            catch (ThreadAbortException)
            {
            }
#endif
            catch (Exception exception)
            {
                _logger.Error(exception, "Error while maintaining connection.");
            }
            finally
            {
                IsStarted = false;

                await UpdateConnectionStatus(MaintainConnectionResult.NotConnected).ConfigureAwait(false);
            }

            // TODO: Disconnected!
        }
    }
}