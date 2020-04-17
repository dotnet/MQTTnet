using MQTTnet.Adapter;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.ExtendedAuthenticationExchange;
using MQTTnet.Client.Options;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Receiving;
using MQTTnet.Client.Subscribing;
using MQTTnet.Client.Unsubscribing;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Internal;
using MQTTnet.PacketDispatcher;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Client
{
    public class MqttClient : Disposable, IMqttClient
    {
        readonly MqttPacketIdentifierProvider _packetIdentifierProvider = new MqttPacketIdentifierProvider();
        readonly MqttPacketDispatcher _packetDispatcher = new MqttPacketDispatcher();
        readonly Stopwatch _sendTracker = new Stopwatch();
        readonly Stopwatch _receiveTracker = new Stopwatch();
        readonly object _disconnectLock = new object();

        readonly IMqttClientAdapterFactory _adapterFactory;
        readonly IMqttNetLogger _logger;

        CancellationTokenSource _backgroundCancellationTokenSource;
        Task _packetReceiverTask;
        Task _keepAlivePacketsSenderTask;
        Task _publishPacketReceiverTask;

        AsyncQueue<MqttPublishPacket> _publishPacketReceiverQueue;

        IMqttChannelAdapter _adapter;
        bool _cleanDisconnectInitiated;
        long _isDisconnectPending;
        bool _isConnected;

        public MqttClient(IMqttClientAdapterFactory channelFactory, IMqttNetLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            _adapterFactory = channelFactory ?? throw new ArgumentNullException(nameof(channelFactory));
            _logger = logger.CreateChildLogger(nameof(MqttClient));
        }

        public IMqttClientConnectedHandler ConnectedHandler { get; set; }

        public IMqttClientDisconnectedHandler DisconnectedHandler { get; set; }

        public IMqttApplicationMessageReceivedHandler ApplicationMessageReceivedHandler { get; set; }

        public bool IsConnected
        {
            get
            {
                return _isConnected || Interlocked.Read(ref _isDisconnectPending) != 0;
            }
        }

        public IMqttClientOptions Options { get; private set; }

        public async Task<MqttClientAuthenticateResult> ConnectAsync(IMqttClientOptions options, CancellationToken cancellationToken)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (options.ChannelOptions == null) throw new ArgumentException("ChannelOptions are not set.");

            ThrowIfConnected("It is not allowed to connect with a server after the connection is established.");

            ThrowIfDisposed();

            MqttClientAuthenticateResult authenticateResult = null;

            try
            {
                Options = options;

                _packetIdentifierProvider.Reset();
                _packetDispatcher.Reset();

                _backgroundCancellationTokenSource = new CancellationTokenSource();
                var backgroundCancellationToken = _backgroundCancellationTokenSource.Token;

                _isDisconnectPending = 0;
                var adapter = _adapterFactory.CreateClientAdapter(options, _logger);
                _adapter = adapter;

                using (var combined = CancellationTokenSource.CreateLinkedTokenSource(backgroundCancellationToken, cancellationToken))
                {
                    _logger.Verbose($"Trying to connect with server '{options.ChannelOptions}' (Timeout={options.CommunicationTimeout}).");
                    await _adapter.ConnectAsync(options.CommunicationTimeout, combined.Token).ConfigureAwait(false);
                    _logger.Verbose("Connection with server established.");

                    _publishPacketReceiverQueue = new AsyncQueue<MqttPublishPacket>();
                    _publishPacketReceiverTask = Task.Run(() => ProcessReceivedPublishPackets(backgroundCancellationToken), backgroundCancellationToken);

                    _packetReceiverTask = Task.Run(() => TryReceivePacketsAsync(backgroundCancellationToken), backgroundCancellationToken);

                    authenticateResult = await AuthenticateAsync(adapter, options.WillMessage, combined.Token).ConfigureAwait(false);
                }

                _sendTracker.Restart();
                _receiveTracker.Restart();

                if (Options.KeepAlivePeriod != TimeSpan.Zero)
                {
                    _keepAlivePacketsSenderTask = Task.Run(() => TrySendKeepAliveMessagesAsync(backgroundCancellationToken), backgroundCancellationToken);
                }

                _isConnected = true;

                _logger.Info("Connected.");

                var connectedHandler = ConnectedHandler;
                if (connectedHandler != null)
                {
                    await connectedHandler.HandleConnectedAsync(new MqttClientConnectedEventArgs(authenticateResult)).ConfigureAwait(false);
                }

                return authenticateResult;
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Error while connecting with server.");

                if (!DisconnectIsPending())
                {
                    await DisconnectInternalAsync(null, exception, authenticateResult).ConfigureAwait(false);
                }

                throw;
            }
        }

        public async Task DisconnectAsync(MqttClientDisconnectOptions options, CancellationToken cancellationToken)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));

            ThrowIfDisposed();

            try
            {
                _cleanDisconnectInitiated = true;

                if (IsConnected)
                {
                    var disconnectPacket = _adapter.PacketFormatterAdapter.DataConverter.CreateDisconnectPacket(options);
                    await SendAsync(disconnectPacket, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                if (!DisconnectIsPending())
                {
                    await DisconnectInternalAsync(null, null, null).ConfigureAwait(false);
                }
            }
        }

        public async Task PingAsync(CancellationToken cancellationToken)
        {
            await SendAndReceiveAsync<MqttPingRespPacket>(new MqttPingReqPacket(), cancellationToken).ConfigureAwait(false);
        }

        public Task SendExtendedAuthenticationExchangeDataAsync(MqttExtendedAuthenticationExchangeData data, CancellationToken cancellationToken)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            ThrowIfDisposed();
            ThrowIfNotConnected();

            return SendAsync(new MqttAuthPacket
            {
                Properties = new MqttAuthPacketProperties
                {
                    // This must always be equal to the value from the CONNECT packet. So we use it here to ensure that.
                    AuthenticationMethod = Options.AuthenticationMethod,
                    AuthenticationData = data.AuthenticationData,
                    ReasonString = data.ReasonString,
                    UserProperties = data.UserProperties
                }
            }, cancellationToken);
        }

        public async Task<MqttClientSubscribeResult> SubscribeAsync(MqttClientSubscribeOptions options, CancellationToken cancellationToken)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            ThrowIfDisposed();
            ThrowIfNotConnected();

            var subscribePacket = _adapter.PacketFormatterAdapter.DataConverter.CreateSubscribePacket(options);
            subscribePacket.PacketIdentifier = _packetIdentifierProvider.GetNextPacketIdentifier();

            var subAckPacket = await SendAndReceiveAsync<MqttSubAckPacket>(subscribePacket, cancellationToken).ConfigureAwait(false);
            return _adapter.PacketFormatterAdapter.DataConverter.CreateClientSubscribeResult(subscribePacket, subAckPacket);
        }

        public async Task<MqttClientUnsubscribeResult> UnsubscribeAsync(MqttClientUnsubscribeOptions options, CancellationToken cancellationToken)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            ThrowIfDisposed();
            ThrowIfNotConnected();

            var unsubscribePacket = _adapter.PacketFormatterAdapter.DataConverter.CreateUnsubscribePacket(options);
            unsubscribePacket.PacketIdentifier = _packetIdentifierProvider.GetNextPacketIdentifier();

            var unsubAckPacket = await SendAndReceiveAsync<MqttUnsubAckPacket>(unsubscribePacket, cancellationToken).ConfigureAwait(false);
            return _adapter.PacketFormatterAdapter.DataConverter.CreateClientUnsubscribeResult(unsubscribePacket, unsubAckPacket);
        }

        public Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            MqttTopicValidator.ThrowIfInvalid(applicationMessage.Topic);

            ThrowIfDisposed();
            ThrowIfNotConnected();

            var publishPacket = _adapter.PacketFormatterAdapter.DataConverter.CreatePublishPacket(applicationMessage);

            switch (applicationMessage.QualityOfServiceLevel)
            {
                case MqttQualityOfServiceLevel.AtMostOnce:
                    {
                        return PublishAtMostOnce(publishPacket, cancellationToken);
                    }
                case MqttQualityOfServiceLevel.AtLeastOnce:
                    {
                        return PublishAtLeastOnceAsync(publishPacket, cancellationToken);
                    }
                case MqttQualityOfServiceLevel.ExactlyOnce:
                    {
                        return PublishExactlyOnceAsync(publishPacket, cancellationToken);
                    }
                default:
                    {
                        throw new NotSupportedException();
                    }
            }
        }

        void Cleanup()
        {
            _backgroundCancellationTokenSource?.Cancel(false);
            _backgroundCancellationTokenSource?.Dispose();
            _backgroundCancellationTokenSource = null;

            _publishPacketReceiverQueue?.Dispose();
            _publishPacketReceiverQueue = null;

            _adapter?.Dispose();
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Cleanup();

                DisconnectedHandler = null;
            }

            base.Dispose(disposing);
        }

        async Task<MqttClientAuthenticateResult> AuthenticateAsync(IMqttChannelAdapter channelAdapter, MqttApplicationMessage willApplicationMessage, CancellationToken cancellationToken)
        {
            var connectPacket = channelAdapter.PacketFormatterAdapter.DataConverter.CreateConnectPacket(
                willApplicationMessage,
                Options);

            var connAckPacket = await SendAndReceiveAsync<MqttConnAckPacket>(connectPacket, cancellationToken).ConfigureAwait(false);
            var result = channelAdapter.PacketFormatterAdapter.DataConverter.CreateClientConnectResult(connAckPacket);

            if (result.ResultCode != MqttClientConnectResultCode.Success)
            {
                throw new MqttConnectingFailedException(result);
            }

            _logger.Verbose("Authenticated MQTT connection with server established.");

            return result;
        }

        void ThrowIfNotConnected()
        {
            if (!IsConnected || Interlocked.Read(ref _isDisconnectPending) == 1) throw new MqttCommunicationException("The client is not connected.");
        }

        void ThrowIfConnected(string message)
        {
            if (IsConnected) throw new MqttProtocolViolationException(message);
        }

        async Task DisconnectInternalAsync(Task sender, Exception exception, MqttClientAuthenticateResult authenticateResult)
        {
            var clientWasConnected = IsConnected;

            TryInitiateDisconnect();
            _isConnected = false;

            try
            {
                if (_adapter != null)
                {
                    _logger.Verbose("Disconnecting [Timeout={0}]", Options.CommunicationTimeout);
                    await _adapter.DisconnectAsync(Options.CommunicationTimeout, CancellationToken.None).ConfigureAwait(false);
                }

                _logger.Verbose("Disconnected from adapter.");
            }
            catch (Exception adapterException)
            {
                _logger.Warning(adapterException, "Error while disconnecting from adapter.");
            }

            try
            {
                var receiverTask = WaitForTaskAsync(_packetReceiverTask, sender);
                var publishPacketReceiverTask = WaitForTaskAsync(_publishPacketReceiverTask, sender);
                var keepAliveTask = WaitForTaskAsync(_keepAlivePacketsSenderTask, sender);

                await Task.WhenAll(receiverTask, publishPacketReceiverTask, keepAliveTask).ConfigureAwait(false);

                _publishPacketReceiverQueue.Dispose();
            }
            catch (Exception e)
            {
                _logger.Warning(e, "Error while waiting for internal tasks.");
            }
            finally
            {
                Cleanup();
                _cleanDisconnectInitiated = false;

                _logger.Info("Disconnected.");

                var disconnectedHandler = DisconnectedHandler;
                if (disconnectedHandler != null)
                {
                    // This handler must be executed in a new thread because otherwise a dead lock may happen
                    // when trying to reconnect in that handler etc.
                    Task.Run(() => disconnectedHandler.HandleDisconnectedAsync(new MqttClientDisconnectedEventArgs(clientWasConnected, exception, authenticateResult))).Forget(_logger);
                }
            }
        }

        void TryInitiateDisconnect()
        {
            lock (_disconnectLock)
            {
                try
                {
                    if (_backgroundCancellationTokenSource?.IsCancellationRequested == true)
                    {
                        return;
                    }

                    _backgroundCancellationTokenSource?.Cancel(false);
                }
                catch (Exception exception)
                {
                    _logger.Warning(exception, "Error while initiating disconnect.");
                }
            }
        }

        private Task SendAsync(MqttBasePacket packet, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromResult(0);
            }

            _sendTracker.Restart();

            return _adapter.SendPacketAsync(packet, Options.CommunicationTimeout, cancellationToken);
        }

        async Task<TResponsePacket> SendAndReceiveAsync<TResponsePacket>(MqttBasePacket requestPacket, CancellationToken cancellationToken) where TResponsePacket : MqttBasePacket
        {
            cancellationToken.ThrowIfCancellationRequested();

            _sendTracker.Restart();

            ushort identifier = 0;
            if (requestPacket is IMqttPacketWithIdentifier packetWithIdentifier && packetWithIdentifier.PacketIdentifier.HasValue)
            {
                identifier = packetWithIdentifier.PacketIdentifier.Value;
            }

            using (var packetAwaiter = _packetDispatcher.AddPacketAwaiter<TResponsePacket>(identifier))
            {
                try
                {
                    await _adapter.SendPacketAsync(requestPacket, Options.CommunicationTimeout, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.Warning(e, "Error when sending packet of type '{0}'.", typeof(TResponsePacket).Name);
                    packetAwaiter.Cancel();
                }

                try
                {
                    var response = await packetAwaiter.WaitOneAsync(Options.CommunicationTimeout).ConfigureAwait(false);
                    _receiveTracker.Restart();
                    return response;
                }
                catch (Exception exception)
                {
                    if (exception is MqttCommunicationTimedOutException)
                    {
                        _logger.Warning(null, "Timeout while waiting for packet of type '{0}'.", typeof(TResponsePacket).Name);
                    }

                    throw;
                }
            }
        }

        async Task TrySendKeepAliveMessagesAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.Verbose("Start sending keep alive packets.");

                while (!cancellationToken.IsCancellationRequested)
                {
                    // Values described here: [MQTT-3.1.2-24].
                    var keepAliveSendInterval = TimeSpan.FromSeconds(Options.KeepAlivePeriod.TotalSeconds * 0.75);
                    if (Options.KeepAliveSendInterval.HasValue)
                    {
                        keepAliveSendInterval = Options.KeepAliveSendInterval.Value;
                    }

                    var waitTimeSend = keepAliveSendInterval - _sendTracker.Elapsed;
                    var waitTimeReceive = keepAliveSendInterval - _receiveTracker.Elapsed;
                    if (waitTimeSend <= TimeSpan.Zero || waitTimeReceive <= TimeSpan.Zero)
                    {
                        await SendAndReceiveAsync<MqttPingRespPacket>(new MqttPingReqPacket(), cancellationToken).ConfigureAwait(false);
                    }

                    await Task.Delay(keepAliveSendInterval, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                if (_cleanDisconnectInitiated)
                {
                    return;
                }

                if (exception is OperationCanceledException)
                {
                }
                else if (exception is MqttCommunicationException)
                {
                    _logger.Warning(exception, "Communication error while sending/receiving keep alive packets.");
                }
                else
                {
                    _logger.Error(exception, "Error exception while sending/receiving keep alive packets.");
                }

                if (!DisconnectIsPending())
                {
                    await DisconnectInternalAsync(_keepAlivePacketsSenderTask, exception, null).ConfigureAwait(false);
                }
            }
            finally
            {
                _logger.Verbose("Stopped sending keep alive packets.");
            }
        }

        async Task TryReceivePacketsAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.Verbose("Start receiving packets.");

                while (!cancellationToken.IsCancellationRequested)
                {
                    var packet = await _adapter.ReceivePacketAsync(TimeSpan.Zero, cancellationToken).ConfigureAwait(false);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    if (packet == null)
                    {
                        if (!DisconnectIsPending())
                        {
                            await DisconnectInternalAsync(_packetReceiverTask, null, null).ConfigureAwait(false);
                        }

                        return;
                    }

                    await TryProcessReceivedPacketAsync(packet, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                if (_cleanDisconnectInitiated)
                {
                    return;
                }

                if (exception is OperationCanceledException)
                {
                }
                else if (exception is MqttCommunicationException)
                {
                    _logger.Warning(exception, "Communication error while receiving packets.");
                }
                else
                {
                    _logger.Error(exception, "Error while receiving packets.");
                }

                _packetDispatcher.Dispatch(exception);

                if (!DisconnectIsPending())
                {
                    await DisconnectInternalAsync(_packetReceiverTask, exception, null).ConfigureAwait(false);
                }
            }
            finally
            {
                _logger.Verbose("Stopped receiving packets.");
            }
        }

        async Task TryProcessReceivedPacketAsync(MqttBasePacket packet, CancellationToken cancellationToken)
        {
            try
            {
                _receiveTracker.Restart();

                if (packet is MqttPublishPacket publishPacket)
                {
                    EnqueueReceivedPublishPacket(publishPacket);
                }
                else if (packet is MqttPubRelPacket pubRelPacket)
                {
                    await SendAsync(new MqttPubCompPacket
                    {
                        PacketIdentifier = pubRelPacket.PacketIdentifier,
                        ReasonCode = MqttPubCompReasonCode.Success
                    }, cancellationToken).ConfigureAwait(false);
                }
                else if (packet is MqttPingReqPacket)
                {
                    await SendAsync(new MqttPingRespPacket(), cancellationToken).ConfigureAwait(false);
                }
                else if (packet is MqttDisconnectPacket)
                {
                    // Also dispatch disconnect to waiting threads to generate a proper exception.
                    _packetDispatcher.Dispatch(packet);

                    await DisconnectAsync(null, cancellationToken).ConfigureAwait(false);
                }
                else if (packet is MqttAuthPacket authPacket)
                {
                    var extendedAuthenticationExchangeHandler = Options.ExtendedAuthenticationExchangeHandler;
                    if (extendedAuthenticationExchangeHandler != null)
                    {
                        await extendedAuthenticationExchangeHandler.HandleRequestAsync(new MqttExtendedAuthenticationExchangeContext(authPacket, this)).ConfigureAwait(false);
                    }
                }
                else
                {
                    _packetDispatcher.Dispatch(packet);
                }
            }
            catch (Exception exception)
            {
                if (_cleanDisconnectInitiated)
                {
                    return;
                }

                if (exception is OperationCanceledException)
                {
                }
                else if (exception is MqttCommunicationException)
                {
                    _logger.Warning(exception, "Communication error while receiving packets.");
                }
                else
                {
                    _logger.Error(exception, "Error while receiving packets.");
                }

                _packetDispatcher.Dispatch(exception);

                if (!DisconnectIsPending())
                {
                    await DisconnectInternalAsync(_packetReceiverTask, exception, null).ConfigureAwait(false);
                }
            }
        }

        void EnqueueReceivedPublishPacket(MqttPublishPacket publishPacket)
        {
            try
            {
                _publishPacketReceiverQueue.Enqueue(publishPacket);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Error while enqueueing application message.");
            }
        }

        async Task ProcessReceivedPublishPackets(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var publishPacketDequeueResult = await _publishPacketReceiverQueue.TryDequeueAsync(cancellationToken);

                    if (!publishPacketDequeueResult.IsSuccess)
                    {
                        return;
                    }

                    var publishPacket = publishPacketDequeueResult.Item;

                    if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtMostOnce)
                    {
                        await HandleReceivedApplicationMessageAsync(publishPacket).ConfigureAwait(false);
                    }
                    else if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtLeastOnce)
                    {
                        if (await HandleReceivedApplicationMessageAsync(publishPacket).ConfigureAwait(false))
                        {
                            await SendAsync(new MqttPubAckPacket
                            {
                                PacketIdentifier = publishPacket.PacketIdentifier,
                                ReasonCode = MqttPubAckReasonCode.Success
                            }, cancellationToken).ConfigureAwait(false);
                        }
                    }
                    else if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.ExactlyOnce)
                    {
                        if (await HandleReceivedApplicationMessageAsync(publishPacket).ConfigureAwait(false))
                        {
                            var pubRecPacket = new MqttPubRecPacket
                            {
                                PacketIdentifier = publishPacket.PacketIdentifier,
                                ReasonCode = MqttPubRecReasonCode.Success
                            };

                            await SendAsync(pubRecPacket, cancellationToken).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        throw new MqttProtocolViolationException("Received a not supported QoS level.");
                    }
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, "Error while handling application message.");
                }
            }
        }

        async Task<MqttClientPublishResult> PublishAtMostOnce(MqttPublishPacket publishPacket, CancellationToken cancellationToken)
        {
            // No packet identifier is used for QoS 0 [3.3.2.2 Packet Identifier]
            await SendAsync(publishPacket, cancellationToken).ConfigureAwait(false);
            return _adapter.PacketFormatterAdapter.DataConverter.CreatePublishResult(null);
        }

        async Task<MqttClientPublishResult> PublishAtLeastOnceAsync(MqttPublishPacket publishPacket, CancellationToken cancellationToken)
        {
            publishPacket.PacketIdentifier = _packetIdentifierProvider.GetNextPacketIdentifier();
            var response = await SendAndReceiveAsync<MqttPubAckPacket>(publishPacket, cancellationToken).ConfigureAwait(false);
            return _adapter.PacketFormatterAdapter.DataConverter.CreatePublishResult(response);
        }

        async Task<MqttClientPublishResult> PublishExactlyOnceAsync(MqttPublishPacket publishPacket, CancellationToken cancellationToken)
        {
            publishPacket.PacketIdentifier = _packetIdentifierProvider.GetNextPacketIdentifier();

            var pubRecPacket = await SendAndReceiveAsync<MqttPubRecPacket>(publishPacket, cancellationToken).ConfigureAwait(false);

            var pubRelPacket = new MqttPubRelPacket
            {
                PacketIdentifier = publishPacket.PacketIdentifier,
                ReasonCode = MqttPubRelReasonCode.Success
            };

            var pubCompPacket = await SendAndReceiveAsync<MqttPubCompPacket>(pubRelPacket, cancellationToken).ConfigureAwait(false);

            return _adapter.PacketFormatterAdapter.DataConverter.CreatePublishResult(pubRecPacket, pubCompPacket);
        }

        async Task<bool> HandleReceivedApplicationMessageAsync(MqttPublishPacket publishPacket)
        {
            var applicationMessage = _adapter.PacketFormatterAdapter.DataConverter.CreateApplicationMessage(publishPacket);

            var handler = ApplicationMessageReceivedHandler;
            if (handler != null)
            {
                var eventArgs = new MqttApplicationMessageReceivedEventArgs(Options.ClientId, applicationMessage);
                await handler.HandleApplicationMessageReceivedAsync(eventArgs).ConfigureAwait(false);
                return !eventArgs.ProcessingFailed;
            }

            return true;
        }

        async Task WaitForTaskAsync(Task task, Task sender)
        {
            if (task == null)
            {
                return;
            }

            if (task == sender)
            {
                // Return here to avoid deadlocks, but first any eventual exception in the task
                // must be handled to avoid not getting an unhandled task exception
                if (!task.IsFaulted)
                {
                    return;
                }

                // By accessing the Exception property the exception is considered handled and will
                // not result in an unhandled task exception later by the finalizer
                _logger.Warning(task.Exception, "Error while waiting for background task.");
                return;
            }

            try
            {
                await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        bool DisconnectIsPending()
        {
            return Interlocked.CompareExchange(ref _isDisconnectPending, 1, 0) != 0;
        }
    }
}
