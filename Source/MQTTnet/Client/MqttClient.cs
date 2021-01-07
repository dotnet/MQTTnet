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
using MQTTnet.Implementations;

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
        readonly IMqttNetScopedLogger _logger;

        CancellationTokenSource _backgroundCancellationTokenSource;
        Task _packetReceiverTask;
        Task _keepAlivePacketsSenderTask;
        Task _publishPacketReceiverTask;

        AsyncQueue<MqttPublishPacket> _publishPacketReceiverQueue;

        IMqttChannelAdapter _adapter;
        bool _cleanDisconnectInitiated;
        long _isDisconnectPending;
        bool _isConnected;
        MqttClientDisconnectReason _disconnectReason;

        public MqttClient(IMqttClientAdapterFactory channelFactory, IMqttNetLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            _adapterFactory = channelFactory ?? throw new ArgumentNullException(nameof(channelFactory));
            _logger = logger.CreateScopedLogger(nameof(MqttClient));
        }

        public IMqttClientConnectedHandler ConnectedHandler { get; set; }

        public IMqttClientDisconnectedHandler DisconnectedHandler { get; set; }

        public IMqttApplicationMessageReceivedHandler ApplicationMessageReceivedHandler { get; set; }

        public bool IsConnected => _isConnected && Interlocked.Read(ref _isDisconnectPending) == 0;

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
                _packetDispatcher.Cancel();

                _backgroundCancellationTokenSource = new CancellationTokenSource();
                var backgroundCancellationToken = _backgroundCancellationTokenSource.Token;

                _isDisconnectPending = 0;
                var adapter = _adapterFactory.CreateClientAdapter(options);
                _adapter = adapter;

                using (var combined = CancellationTokenSource.CreateLinkedTokenSource(backgroundCancellationToken, cancellationToken))
                {
                    _logger.Verbose("Trying to connect with server '{0}' (Timeout={1}).", options.ChannelOptions, options.CommunicationTimeout);
                    await adapter.ConnectAsync(options.CommunicationTimeout, combined.Token).ConfigureAwait(false);
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
                _disconnectReason = MqttClientDisconnectReason.UnspecifiedError;

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

            if (DisconnectIsPending())
            {
                return;
            }

            try
            {
                _disconnectReason = MqttClientDisconnectReason.NormalDisconnection;
                _cleanDisconnectInitiated = true;

                if (_isConnected)
                {
                    var disconnectPacket = _adapter.PacketFormatterAdapter.DataConverter.CreateDisconnectPacket(options);
                    await SendAsync(disconnectPacket, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                await DisconnectInternalAsync(null, null, null).ConfigureAwait(false);
            }
        }

        public Task PingAsync(CancellationToken cancellationToken)
        {
            return SendAndReceiveAsync<MqttPingRespPacket>(new MqttPingReqPacket(), cancellationToken);
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
            cancellationToken.ThrowIfCancellationRequested();

            MqttTopicValidator.ThrowIfInvalid(applicationMessage);

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
            try
            {
                _backgroundCancellationTokenSource?.Cancel(false);
            }
            finally
            {
                _backgroundCancellationTokenSource?.Dispose();
                _backgroundCancellationTokenSource = null;

                _publishPacketReceiverQueue?.Dispose();
                _publishPacketReceiverQueue = null;

                _adapter?.Dispose();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Cleanup();
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
            if (!IsConnected || Interlocked.Read(ref _isDisconnectPending) == 1)
            {
                throw new MqttCommunicationException("The client is not connected.");
            }
        }

        void ThrowIfConnected(string message)
        {
            if (IsConnected) throw new MqttProtocolViolationException(message);
        }

        async Task DisconnectInternalAsync(Task sender, Exception exception, MqttClientAuthenticateResult authenticateResult)
        {
            var clientWasConnected = _isConnected;

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

                _publishPacketReceiverQueue?.Dispose();
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
                    Task.Run(() => disconnectedHandler.HandleDisconnectedAsync(new MqttClientDisconnectedEventArgs(clientWasConnected, exception, authenticateResult, _disconnectReason))).RunInBackground(_logger);
                }
            }
        }

        void TryInitiateDisconnect()
        {
            lock (_disconnectLock)
            {
                try
                {
                    _backgroundCancellationTokenSource?.Cancel(false);
                }
                catch (Exception exception)
                {
                    _logger.Warning(exception, "Error while initiating disconnect.");
                }
            }
        }

        Task SendAsync(MqttBasePacket packet, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _sendTracker.Restart();

            return _adapter.SendPacketAsync(packet, cancellationToken);
        }

        async Task<TResponsePacket> SendAndReceiveAsync<TResponsePacket>(MqttBasePacket requestPacket, CancellationToken cancellationToken) where TResponsePacket : MqttBasePacket
        {
            cancellationToken.ThrowIfCancellationRequested();

            ushort identifier = 0;
            if (requestPacket is IMqttPacketWithIdentifier packetWithIdentifier && packetWithIdentifier.PacketIdentifier > 0)
            {
                identifier = packetWithIdentifier.PacketIdentifier;
            }

            using (var packetAwaiter = _packetDispatcher.AddAwaiter<TResponsePacket>(identifier))
            {
                try
                {
                    _sendTracker.Restart();
                    await _adapter.SendPacketAsync(requestPacket, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.Warning(e, "Error when sending request packet ({0}).", requestPacket.GetType().Name);
                    packetAwaiter.Cancel();
                }

                try
                {
                    return await packetAwaiter.WaitOneAsync(Options.CommunicationTimeout).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    if (exception is MqttCommunicationTimedOutException)
                    {
                        _logger.Warning(null, "Timeout while waiting for response packet ({0}).", typeof(TResponsePacket).Name);
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

                var keepAlivePeriod = Options.KeepAlivePeriod;

                while (!cancellationToken.IsCancellationRequested)
                {
                    // Values described here: [MQTT-3.1.2-24].
                    var waitTime = keepAlivePeriod - _sendTracker.Elapsed;

                    if (waitTime <= TimeSpan.Zero)
                    {
                        await SendAndReceiveAsync<MqttPingRespPacket>(MqttPingReqPacket.Instance, cancellationToken).ConfigureAwait(false);
                    }

                    // Wait a fixed time in all cases. Calculation of the remaining time is complicated
                    // due to some edge cases and was buggy in the past. Now we wait half a second because the
                    // min keep alive value is one second so that the server will wait 1.5 seconds for a PING
                    // packet.
                    await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken).ConfigureAwait(false);
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
                    return;
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
                    var packet = await _adapter.ReceivePacketAsync(cancellationToken).ConfigureAwait(false);

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
                else if (packet is MqttPubRecPacket pubRecPacket)
                {
                    await ProcessReceivedPubRecPacket(pubRecPacket, cancellationToken).ConfigureAwait(false);
                }
                else if (packet is MqttPubRelPacket pubRelPacket)
                {
                    await ProcessReceivedPubRelPacket(pubRelPacket, cancellationToken).ConfigureAwait(false);
                }
                else if (packet is MqttPingReqPacket)
                {
                    await SendAsync(MqttPingRespPacket.Instance, cancellationToken).ConfigureAwait(false);
                }
                else if (packet is MqttDisconnectPacket disconnectPacket)
                {
                    await ProcessReceivedDisconnectPacket(disconnectPacket).ConfigureAwait(false);
                }
                else if (packet is MqttAuthPacket authPacket)
                {
                    await ProcessReceivedAuthPacket(authPacket).ConfigureAwait(false);
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
                _logger.Error(exception, "Error while queueing application message.");
            }
        }

        async Task ProcessReceivedPublishPackets(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var publishPacketDequeueResult = await _publishPacketReceiverQueue.TryDequeueAsync(cancellationToken).ConfigureAwait(false);
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
                catch (OperationCanceledException)
                {
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, "Error while handling application message.");
                }
            }
        }

        Task ProcessReceivedPubRecPacket(MqttPubRecPacket pubRecPacket, CancellationToken cancellationToken)
        {
            if (!_packetDispatcher.TryDispatch(pubRecPacket))
            {
                // The packet is unknown. Probably due to a restart of the client. 
                // So wen send this to the server to trigger a full resend of the message.
                return SendAsync(new MqttPubRelPacket
                {
                    PacketIdentifier = pubRecPacket.PacketIdentifier,
                    ReasonCode = MqttPubRelReasonCode.PacketIdentifierNotFound
                }, cancellationToken);
            }

            return PlatformAbstractionLayer.CompletedTask;
        }

        Task ProcessReceivedPubRelPacket(MqttPubRelPacket pubRelPacket, CancellationToken cancellationToken)
        {
            return SendAsync(new MqttPubCompPacket
            {
                PacketIdentifier = pubRelPacket.PacketIdentifier,
                ReasonCode = MqttPubCompReasonCode.Success
            }, cancellationToken);
        }

        Task ProcessReceivedDisconnectPacket(MqttDisconnectPacket disconnectPacket)
        {
            _disconnectReason = (MqttClientDisconnectReason)(disconnectPacket.ReasonCode ?? MqttDisconnectReasonCode.NormalDisconnection);

            // Also dispatch disconnect to waiting threads to generate a proper exception.
            _packetDispatcher.Dispatch(disconnectPacket);

            if (!DisconnectIsPending())
            {
                return DisconnectInternalAsync(_packetReceiverTask, null, null);
            }

            return PlatformAbstractionLayer.CompletedTask;
        }

        Task ProcessReceivedAuthPacket(MqttAuthPacket authPacket)
        {
            var extendedAuthenticationExchangeHandler = Options.ExtendedAuthenticationExchangeHandler;
            if (extendedAuthenticationExchangeHandler != null)
            {
                return extendedAuthenticationExchangeHandler.HandleRequestAsync(new MqttExtendedAuthenticationExchangeContext(authPacket, this));
            }

            return PlatformAbstractionLayer.CompletedTask;
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
            // This will read the _isDisconnectPending and set it to "1" afterwards regardless of the value.
            // So the first caller will get a "false" and all subsequent ones will get "true".
            return Interlocked.CompareExchange(ref _isDisconnectPending, 1, 0) != 0;
        }
    }
}
