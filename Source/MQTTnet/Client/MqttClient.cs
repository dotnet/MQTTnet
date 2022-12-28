// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Internal;
using MQTTnet.PacketDispatcher;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Client
{
    public sealed class MqttClient : Disposable, IMqttClient
    {
        readonly IMqttClientAdapterFactory _adapterFactory;
        readonly AsyncEvent<MqttApplicationMessageReceivedEventArgs> _applicationMessageReceivedEvent = new AsyncEvent<MqttApplicationMessageReceivedEventArgs>();

        readonly MqttClientPublishResultFactory _clientPublishResultFactory = new MqttClientPublishResultFactory();
        readonly MqttClientSubscribeResultFactory _clientSubscribeResultFactory = new MqttClientSubscribeResultFactory();
        readonly MqttClientUnsubscribeResultFactory _clientUnsubscribeResultFactory = new MqttClientUnsubscribeResultFactory();

        readonly AsyncEvent<MqttClientConnectedEventArgs> _connectedEvent = new AsyncEvent<MqttClientConnectedEventArgs>();
        readonly AsyncEvent<MqttClientConnectingEventArgs> _connectingEvent = new AsyncEvent<MqttClientConnectingEventArgs>();
        readonly AsyncEvent<MqttClientDisconnectedEventArgs> _disconnectedEvent = new AsyncEvent<MqttClientDisconnectedEventArgs>();
        readonly object _disconnectLock = new object();
        readonly AsyncEvent<InspectMqttPacketEventArgs> _inspectPacketEvent = new AsyncEvent<InspectMqttPacketEventArgs>();
        readonly MqttNetSourceLogger _logger;

        readonly MqttPacketIdentifierProvider _packetIdentifierProvider = new MqttPacketIdentifierProvider();
        readonly IMqttNetLogger _rootLogger;

        IMqttChannelAdapter _adapter;

        bool _cleanDisconnectInitiated;
        volatile int _connectionStatus;

        // The value for this field can be set from two different enums.
        // They contain the same values but the set is reduced in one case.
        int _disconnectReason;
        string _disconnectReasonString;
        List<MqttUserProperty> _disconnectUserProperties;

        Task _keepAlivePacketsSenderTask;
        DateTime _lastPacketSentTimestamp;

        CancellationTokenSource _mqttClientAlive;
        MqttPacketDispatcher _packetDispatcher;
        Task _packetReceiverTask;
        AsyncQueue<MqttPublishPacket> _publishPacketReceiverQueue;
        Task _publishPacketReceiverTask;

        public MqttClient(IMqttClientAdapterFactory channelFactory, IMqttNetLogger logger)
        {
            _adapterFactory = channelFactory ?? throw new ArgumentNullException(nameof(channelFactory));
            _rootLogger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger = logger.WithSource(nameof(MqttClient));
        }

        public event Func<MqttApplicationMessageReceivedEventArgs, Task> ApplicationMessageReceivedAsync
        {
            add => _applicationMessageReceivedEvent.AddHandler(value);
            remove => _applicationMessageReceivedEvent.RemoveHandler(value);
        }

        public event Func<MqttClientConnectedEventArgs, Task> ConnectedAsync
        {
            add => _connectedEvent.AddHandler(value);
            remove => _connectedEvent.RemoveHandler(value);
        }

        public event Func<MqttClientConnectingEventArgs, Task> ConnectingAsync
        {
            add => _connectingEvent.AddHandler(value);
            remove => _connectingEvent.RemoveHandler(value);
        }

        public event Func<MqttClientDisconnectedEventArgs, Task> DisconnectedAsync
        {
            add => _disconnectedEvent.AddHandler(value);
            remove => _disconnectedEvent.RemoveHandler(value);
        }

        public event Func<InspectMqttPacketEventArgs, Task> InspectPackage
        {
            add => _inspectPacketEvent.AddHandler(value);
            remove => _inspectPacketEvent.RemoveHandler(value);
        }

        public bool IsConnected => _adapter != null && (MqttClientConnectionStatus)_connectionStatus == MqttClientConnectionStatus.Connected;

        public MqttClientOptions Options { get; private set; }

        public async Task<MqttClientConnectResult> ConnectAsync(MqttClientOptions options, CancellationToken cancellationToken = default)
        {
            ThrowIfOptionsInvalid(options);
            ThrowIfConnected("It is not allowed to connect with a server after the connection is established.");
            ThrowIfDisposed();

            if (CompareExchangeConnectionStatus(MqttClientConnectionStatus.Connecting, MqttClientConnectionStatus.Disconnected) != MqttClientConnectionStatus.Disconnected)
            {
                throw new InvalidOperationException("Not allowed to connect while connect/disconnect is pending.");
            }

            MqttClientConnectResult connectResult = null;

            try
            {
                Options = options;

                if (_connectingEvent.HasHandlers)
                {
                    await _connectingEvent.InvokeAsync(new MqttClientConnectingEventArgs(options)).ConfigureAwait(false);
                }

                Cleanup();

                _packetIdentifierProvider.Reset();
                _packetDispatcher = new MqttPacketDispatcher();

                _mqttClientAlive = new CancellationTokenSource();
                var mqttClientAliveToken = _mqttClientAlive.Token;

                var adapter = _adapterFactory.CreateClientAdapter(options, new MqttPacketInspector(_inspectPacketEvent, _rootLogger), _rootLogger);
                _adapter = adapter;

                if (cancellationToken.CanBeCanceled)
                {
                    connectResult = await ConnectInternal(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // Fall back to the general timeout specified in the options if the user passed
                    // CancellationToken.None or similar.
                    using (var timeout = new CancellationTokenSource(Options.Timeout))
                    {
                        connectResult = await ConnectInternal(timeout.Token).ConfigureAwait(false);
                    }
                }

                _lastPacketSentTimestamp = DateTime.UtcNow;

                var keepAliveInterval = Options.KeepAlivePeriod;
                if (connectResult.ServerKeepAlive > 0)
                {
                    _logger.Info($"Using keep alive value ({connectResult.ServerKeepAlive}) sent from the server.");
                    keepAliveInterval = TimeSpan.FromSeconds(connectResult.ServerKeepAlive);
                }

                if (keepAliveInterval != TimeSpan.Zero)
                {
                    _keepAlivePacketsSenderTask = Task.Run(() => TrySendKeepAliveMessagesAsync(mqttClientAliveToken), mqttClientAliveToken);
                }

                CompareExchangeConnectionStatus(MqttClientConnectionStatus.Connected, MqttClientConnectionStatus.Connecting);

                _logger.Info("Connected.");

                if (_connectedEvent.HasHandlers)
                {
                    var eventArgs = new MqttClientConnectedEventArgs(connectResult);
                    await _connectedEvent.InvokeAsync(eventArgs).ConfigureAwait(false);
                }

                return connectResult;
            }
            catch (Exception exception)
            {
                _disconnectReason = (int)MqttClientDisconnectOptionsReason.UnspecifiedError;

                if (!(exception is OperationCanceledException))
                {
                    _logger.Error(exception, "Error while connecting with server.");
                }

                await DisconnectInternalAsync(null, exception, connectResult).ConfigureAwait(false);

                throw;
            }
        }

        public async Task DisconnectAsync(MqttClientDisconnectOptions options, CancellationToken cancellationToken = default)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            ThrowIfDisposed();

            var clientWasConnected = IsConnected;

            if (DisconnectIsPendingOrFinished())
            {
                return;
            }

            try
            {
                if (!clientWasConnected)
                {
                    ThrowNotConnected();
                }

                _disconnectReason = (int)options.Reason;
                _cleanDisconnectInitiated = true;

                if (Options.ValidateFeatures)
                {
                    MqttClientDisconnectOptionsValidator.ThrowIfNotSupported(options, _adapter.PacketFormatterAdapter.ProtocolVersion);
                }

                // Sending the DISCONNECT may fail due to connection issues. The resulting exception
                // must be throw to let the caller know that the disconnect is not a clean one.
                var disconnectPacket = MqttPacketFactories.Disconnect.Create(options);

                if (cancellationToken.CanBeCanceled)
                {
                    await SendAsync(disconnectPacket, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    using (var timeout = new CancellationTokenSource(Options.Timeout))
                    {
                        await SendAsync(disconnectPacket, timeout.Token).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                await DisconnectCoreAsync(null, null, null, clientWasConnected).ConfigureAwait(false);
            }
        }

        public async Task PingAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.CanBeCanceled)
            {
                await SendAndReceiveAsync<MqttPingRespPacket>(MqttPingReqPacket.Instance, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                using (var timeout = new CancellationTokenSource(Options.Timeout))
                {
                    await SendAndReceiveAsync<MqttPingRespPacket>(MqttPingReqPacket.Instance, timeout.Token).ConfigureAwait(false);
                }
            }
        }

        public Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            MqttTopicValidator.ThrowIfInvalid(applicationMessage);

            ThrowIfDisposed();
            ThrowIfNotConnected();

            if (Options.ValidateFeatures)
            {
                MqttApplicationMessageValidator.ThrowIfNotSupported(applicationMessage, _adapter.PacketFormatterAdapter.ProtocolVersion);
            }

            var publishPacket = MqttPacketFactories.Publish.Create(applicationMessage);

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

        public Task SendExtendedAuthenticationExchangeDataAsync(MqttExtendedAuthenticationExchangeData data, CancellationToken cancellationToken = default)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            ThrowIfDisposed();
            ThrowIfNotConnected();

            var authPacket = new MqttAuthPacket
            {
                // This must always be equal to the value from the CONNECT packet. So we use it here to ensure that.
                AuthenticationMethod = Options.AuthenticationMethod,
                AuthenticationData = data.AuthenticationData,
                ReasonString = data.ReasonString,
                UserProperties = data.UserProperties
            };

            return SendAsync(authPacket, cancellationToken);
        }

        public async Task<MqttClientSubscribeResult> SubscribeAsync(MqttClientSubscribeOptions options, CancellationToken cancellationToken = default)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            foreach (var topicFilter in options.TopicFilters)
            {
                MqttTopicValidator.ThrowIfInvalidSubscribe(topicFilter.Topic);
            }

            if (Options.ValidateFeatures)
            {
                MqttClientSubscribeOptionsValidator.ThrowIfNotSupported(options, _adapter.PacketFormatterAdapter.ProtocolVersion);
            }

            ThrowIfDisposed();
            ThrowIfNotConnected();

            var subscribePacket = MqttPacketFactories.Subscribe.Create(options);
            subscribePacket.PacketIdentifier = _packetIdentifierProvider.GetNextPacketIdentifier();

            MqttSubAckPacket subAckPacket;
            if (cancellationToken.CanBeCanceled)
            {
                subAckPacket = await SendAndReceiveAsync<MqttSubAckPacket>(subscribePacket, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                using (var timeout = new CancellationTokenSource(Options.Timeout))
                {
                    subAckPacket = await SendAndReceiveAsync<MqttSubAckPacket>(subscribePacket, timeout.Token).ConfigureAwait(false);
                }
            }

            return _clientSubscribeResultFactory.Create(subscribePacket, subAckPacket);
        }

        public async Task<MqttClientUnsubscribeResult> UnsubscribeAsync(MqttClientUnsubscribeOptions options, CancellationToken cancellationToken = default)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            foreach (var topicFilter in options.TopicFilters)
            {
                MqttTopicValidator.ThrowIfInvalidSubscribe(topicFilter);
            }

            ThrowIfDisposed();
            ThrowIfNotConnected();

            if (Options.ValidateFeatures)
            {
                MqttClientUnsubscribeOptionsValidator.ThrowIfNotSupported(options, _adapter.PacketFormatterAdapter.ProtocolVersion);
            }

            var unsubscribePacket = MqttPacketFactories.Unsubscribe.Create(options);
            unsubscribePacket.PacketIdentifier = _packetIdentifierProvider.GetNextPacketIdentifier();

            MqttUnsubAckPacket unsubAckPacket;
            if (cancellationToken.CanBeCanceled)
            {
                unsubAckPacket = await SendAndReceiveAsync<MqttUnsubAckPacket>(unsubscribePacket, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                using (var timeout = new CancellationTokenSource(Options.Timeout))
                {
                    unsubAckPacket = await SendAndReceiveAsync<MqttUnsubAckPacket>(unsubscribePacket, timeout.Token).ConfigureAwait(false);
                }
            }

            return _clientUnsubscribeResultFactory.Create(unsubscribePacket, unsubAckPacket);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Cleanup();
            }

            base.Dispose(disposing);
        }

        Task AcknowledgeReceivedPublishPacket(MqttApplicationMessageReceivedEventArgs eventArgs, CancellationToken cancellationToken)
        {
            if (eventArgs.PublishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtMostOnce)
            {
                // no response required
            }
            else if (eventArgs.PublishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtLeastOnce)
            {
                if (!eventArgs.ProcessingFailed)
                {
                    var pubAckPacket = MqttPacketFactories.PubAck.Create(eventArgs);
                    return SendAsync(pubAckPacket, cancellationToken);
                }
            }
            else if (eventArgs.PublishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.ExactlyOnce)
            {
                if (!eventArgs.ProcessingFailed)
                {
                    var pubRecPacket = MqttPacketFactories.PubRec.Create(eventArgs);
                    return SendAsync(pubRecPacket, cancellationToken);
                }
            }
            else
            {
                throw new MqttProtocolViolationException("Received a not supported QoS level.");
            }

            return CompletedTask.Instance;
        }

        async Task<MqttClientConnectResult> AuthenticateAsync(MqttClientOptions options, CancellationToken cancellationToken)
        {
            MqttClientConnectResult result;

            try
            {
                var connectPacket = MqttPacketFactories.Connect.Create(options);

                var connAckPacket = await SendAndReceiveAsync<MqttConnAckPacket>(connectPacket, cancellationToken).ConfigureAwait(false);

                var clientConnectResultFactory = new MqttClientConnectResultFactory();
                result = clientConnectResultFactory.Create(connAckPacket, _adapter.PacketFormatterAdapter.ProtocolVersion);
            }
            catch (Exception exception)
            {
                throw new MqttConnectingFailedException($"Error while authenticating. {exception.Message}", exception, null);
            }

            if (result.ResultCode != MqttClientConnectResultCode.Success)
            {
                throw new MqttConnectingFailedException($"Connecting with MQTT server failed ({result.ResultCode}).", null, result);
            }

            _logger.Verbose("Authenticated MQTT connection with server established.");

            return result;
        }

        void Cleanup()
        {
            try
            {
                _mqttClientAlive?.Cancel(false);
            }
            finally
            {
                _mqttClientAlive?.Dispose();
                _mqttClientAlive = null;

                _publishPacketReceiverQueue?.Dispose();
                _publishPacketReceiverQueue = null;

                _adapter?.Dispose();
                _adapter = null;

                _packetDispatcher?.Dispose();
                _packetDispatcher = null;
            }
        }

        MqttClientConnectionStatus CompareExchangeConnectionStatus(MqttClientConnectionStatus value, MqttClientConnectionStatus comparand)
        {
            return (MqttClientConnectionStatus)Interlocked.CompareExchange(ref _connectionStatus, (int)value, (int)comparand);
        }

        async Task<MqttClientConnectResult> ConnectInternal(CancellationToken cancellationToken)
        {
            var backgroundCancellationToken = _mqttClientAlive.Token;

            using (var effectiveCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(backgroundCancellationToken, cancellationToken))
            {
                _logger.Verbose("Trying to connect with server '{0}'.", Options.ChannelOptions);
                await _adapter.ConnectAsync(effectiveCancellationToken.Token).ConfigureAwait(false);
                _logger.Verbose("Connection with server established.");

                _publishPacketReceiverQueue?.Dispose();
                _publishPacketReceiverQueue = new AsyncQueue<MqttPublishPacket>();

                _publishPacketReceiverTask = Task.Run(() => ProcessReceivedPublishPackets(backgroundCancellationToken), backgroundCancellationToken);
                _packetReceiverTask = Task.Run(() => TryReceivePacketsAsync(backgroundCancellationToken), backgroundCancellationToken);

                return await AuthenticateAsync(Options, effectiveCancellationToken.Token).ConfigureAwait(false);
            }
        }

        async Task DisconnectCoreAsync(Task sender, Exception exception, MqttClientConnectResult connectResult, bool clientWasConnected)
        {
            TryInitiateDisconnect();

            try
            {
                if (_adapter != null)
                {
                    _logger.Verbose("Disconnecting [Timeout={0}]", Options.Timeout);

                    using (var timeout = new CancellationTokenSource(Options.Timeout))
                    {
                        await _adapter.DisconnectAsync(timeout.Token).ConfigureAwait(false);
                    }
                }

                _logger.Verbose("Disconnected from adapter.");
            }
            catch (Exception adapterException)
            {
                _logger.Warning(adapterException, "Error while disconnecting from adapter.");
            }

            try
            {
                _packetDispatcher.Dispose(new MqttClientDisconnectedException(exception));

                var receiverTask = _packetReceiverTask.WaitAsync(sender, _logger);
                var publishPacketReceiverTask = _publishPacketReceiverTask.WaitAsync(sender, _logger);
                var keepAliveTask = _keepAlivePacketsSenderTask.WaitAsync(sender, _logger);

                await Task.WhenAll(receiverTask, publishPacketReceiverTask, keepAliveTask).ConfigureAwait(false);
            }
            catch (Exception innerException)
            {
                _logger.Warning(innerException, "Error while waiting for internal tasks.");
            }
            finally
            {
                Cleanup();
                _cleanDisconnectInitiated = false;
                CompareExchangeConnectionStatus(MqttClientConnectionStatus.Disconnected, MqttClientConnectionStatus.Disconnecting);

                _logger.Info("Disconnected.");

                var eventArgs = new MqttClientDisconnectedEventArgs(
                    clientWasConnected,
                    connectResult,
                    (MqttClientDisconnectReason)_disconnectReason,
                    _disconnectReasonString,
                    _disconnectUserProperties,
                    exception);

                // This handler must be executed in a new thread because otherwise a dead lock may happen
                // when trying to reconnect in that handler etc.
                Task.Run(() => _disconnectedEvent.InvokeAsync(eventArgs)).RunInBackground(_logger);
            }
        }

        Task DisconnectInternalAsync(Task sender, Exception exception, MqttClientConnectResult connectResult)
        {
            var clientWasConnected = IsConnected;

            if (!DisconnectIsPendingOrFinished())
            {
                return DisconnectCoreAsync(sender, exception, connectResult, clientWasConnected);
            }

            return CompletedTask.Instance;
        }

        bool DisconnectIsPendingOrFinished()
        {
            var connectionStatus = (MqttClientConnectionStatus)_connectionStatus;

            do
            {
                switch (connectionStatus)
                {
                    case MqttClientConnectionStatus.Disconnected:
                    case MqttClientConnectionStatus.Disconnecting:
                        return true;
                    case MqttClientConnectionStatus.Connected:
                    case MqttClientConnectionStatus.Connecting:
                        // This will compare the _connectionStatus to old value and set it to "MqttClientConnectionStatus.Disconnecting" afterwards.
                        // So the first caller will get a "false" and all subsequent ones will get "true".
                        var curStatus = CompareExchangeConnectionStatus(MqttClientConnectionStatus.Disconnecting, connectionStatus);
                        if (curStatus == connectionStatus)
                        {
                            return false;
                        }

                        connectionStatus = curStatus;
                        break;
                }
            }
            while (true);
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

        async Task<MqttApplicationMessageReceivedEventArgs> HandleReceivedApplicationMessageAsync(MqttPublishPacket publishPacket)
        {
            var applicationMessage = MqttApplicationMessageFactory.Create(publishPacket);
            var eventArgs = new MqttApplicationMessageReceivedEventArgs(Options.ClientId, applicationMessage, publishPacket, AcknowledgeReceivedPublishPacket);
            await _applicationMessageReceivedEvent.InvokeAsync(eventArgs).ConfigureAwait(false);

            return eventArgs;
        }

        Task ProcessReceivedAuthPacket(MqttAuthPacket authPacket)
        {
            var extendedAuthenticationExchangeHandler = Options.ExtendedAuthenticationExchangeHandler;
            if (extendedAuthenticationExchangeHandler != null)
            {
                return extendedAuthenticationExchangeHandler.HandleRequestAsync(new MqttExtendedAuthenticationExchangeContext(authPacket, this));
            }

            return CompletedTask.Instance;
        }

        Task ProcessReceivedDisconnectPacket(MqttDisconnectPacket disconnectPacket)
        {
            _disconnectReason = (int)disconnectPacket.ReasonCode;
            _disconnectReasonString = disconnectPacket.ReasonString;
            _disconnectUserProperties = disconnectPacket.UserProperties;

            // Also dispatch disconnect to waiting threads to generate a proper exception.
            _packetDispatcher.Dispose(new MqttClientUnexpectedDisconnectReceivedException(disconnectPacket));

            return DisconnectInternalAsync(_packetReceiverTask, null, null);
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
                    var eventArgs = await HandleReceivedApplicationMessageAsync(publishPacket).ConfigureAwait(false);

                    if (eventArgs.AutoAcknowledge)
                    {
                        await eventArgs.AcknowledgeAsync(cancellationToken).ConfigureAwait(false);
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
                var pubRelPacket = MqttPacketFactories.PubRel.Create(pubRecPacket, MqttApplicationMessageReceivedReasonCode.PacketIdentifierNotFound);
                return SendAsync(pubRelPacket, cancellationToken);
            }

            return CompletedTask.Instance;
        }

        Task ProcessReceivedPubRelPacket(MqttPubRelPacket pubRelPacket, CancellationToken cancellationToken)
        {
            var pubCompPacket = MqttPacketFactories.PubComp.Create(pubRelPacket, MqttApplicationMessageReceivedReasonCode.Success);
            return SendAsync(pubCompPacket, cancellationToken);
        }

        async Task<MqttClientPublishResult> PublishAtLeastOnceAsync(MqttPublishPacket publishPacket, CancellationToken cancellationToken)
        {
            publishPacket.PacketIdentifier = _packetIdentifierProvider.GetNextPacketIdentifier();

            var pubAckPacket = await SendAndReceiveAsync<MqttPubAckPacket>(publishPacket, cancellationToken).ConfigureAwait(false);
            return _clientPublishResultFactory.Create(pubAckPacket);
        }

        async Task<MqttClientPublishResult> PublishAtMostOnce(MqttPublishPacket publishPacket, CancellationToken cancellationToken)
        {
            // No packet identifier is used for QoS 0 [3.3.2.2 Packet Identifier]
            await SendAsync(publishPacket, cancellationToken).ConfigureAwait(false);

            return _clientPublishResultFactory.Create(null);
        }

        async Task<MqttClientPublishResult> PublishExactlyOnceAsync(MqttPublishPacket publishPacket, CancellationToken cancellationToken)
        {
            publishPacket.PacketIdentifier = _packetIdentifierProvider.GetNextPacketIdentifier();

            var pubRecPacket = await SendAndReceiveAsync<MqttPubRecPacket>(publishPacket, cancellationToken).ConfigureAwait(false);

            var pubRelPacket = MqttPacketFactories.PubRel.Create(pubRecPacket, MqttApplicationMessageReceivedReasonCode.Success);

            var pubCompPacket = await SendAndReceiveAsync<MqttPubCompPacket>(pubRelPacket, cancellationToken).ConfigureAwait(false);

            return _clientPublishResultFactory.Create(pubRecPacket, pubCompPacket);
        }

        async Task<TResponsePacket> SendAndReceiveAsync<TResponsePacket>(MqttPacket requestPacket, CancellationToken cancellationToken) where TResponsePacket : MqttPacket
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfNoAdapter();

            ushort packetIdentifier = 0;
            if (requestPacket is MqttPacketWithIdentifier packetWithIdentifier)
            {
                packetIdentifier = packetWithIdentifier.PacketIdentifier;
            }

            using (var packetAwaitable = _packetDispatcher.AddAwaitable<TResponsePacket>(packetIdentifier))
            {
                try
                {
                    await SendAsync(requestPacket, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    _logger.Warning(exception, "Error when sending request packet ({0}).", requestPacket.GetType().Name);
                    packetAwaitable.Fail(exception);
                }

                try
                {
                    return await packetAwaitable.WaitOneAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    if (exception is MqttCommunicationTimedOutException)
                    {
                        _logger.Warning("Timeout while waiting for response packet ({0}).", typeof(TResponsePacket).Name);
                    }

                    throw;
                }
            }
        }

        Task SendAsync(MqttPacket packet, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfNoAdapter();

            _lastPacketSentTimestamp = DateTime.UtcNow;
            return _adapter.SendPacketAsync(packet, cancellationToken);
        }

        void ThrowIfConnected(string message)
        {
            if (IsConnected)
            {
                throw new InvalidOperationException(message);
            }
        }

        void ThrowIfNoAdapter()
        {
            // Having no adapter indicates that the connection is closed or still being created.
            // Thus the client is disconnected.
            if (_adapter == null)
            {
                ThrowNotConnected();
            }
        }

        void ThrowIfNotConnected()
        {
            if (!IsConnected)
            {
                ThrowNotConnected();
            }
        }

        static void ThrowIfOptionsInvalid(MqttClientOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.ChannelOptions == null)
            {
                throw new ArgumentException("ChannelOptions are not set.");
            }

            if (options.ValidateFeatures)
            {
                MqttClientOptionsValidator.ThrowIfNotSupported(options);
            }
        }

        static void ThrowNotConnected()
        {
            throw new MqttCommunicationException("The client is not connected.");
        }

        void TryInitiateDisconnect()
        {
            lock (_disconnectLock)
            {
                try
                {
                    _mqttClientAlive?.Cancel(false);
                }
                catch (Exception exception)
                {
                    _logger.Warning(exception, "Error while initiating disconnect.");
                }
            }
        }

        async Task TryProcessReceivedPacketAsync(MqttPacket packet, CancellationToken cancellationToken)
        {
            try
            {
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
                else if (packet is MqttDisconnectPacket disconnectPacket)
                {
                    await ProcessReceivedDisconnectPacket(disconnectPacket).ConfigureAwait(false);
                }
                else if (packet is MqttAuthPacket authPacket)
                {
                    await ProcessReceivedAuthPacket(authPacket).ConfigureAwait(false);
                }
                else if (packet is MqttPingRespPacket)
                {
                    _packetDispatcher.TryDispatch(packet);
                }
                else if (packet is MqttPingReqPacket)
                {
                    throw new MqttProtocolViolationException("The PINGREQ Packet is sent from a Client to the Server only.");
                }
                else
                {
                    if (!_packetDispatcher.TryDispatch(packet))
                    {
                        throw new MqttProtocolViolationException($"Received packet '{packet}' at an unexpected time.");
                    }
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
                    _logger.Error(exception, $"Error while processing received packet ({packet.GetType().Name}).");
                }

                _packetDispatcher.FailAll(exception);

                await DisconnectInternalAsync(_packetReceiverTask, exception, null).ConfigureAwait(false);
            }
        }

        async Task TryReceivePacketsAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.Verbose("Start receiving packets.");

                while (!cancellationToken.IsCancellationRequested)
                {
                    MqttPacket packet;
                    var packetTask = _adapter.ReceivePacketAsync(cancellationToken);

                    if (packetTask.IsCompleted)
                    {
                        packet = packetTask.Result;
                    }
                    else
                    {
                        packet = await packetTask.ConfigureAwait(false);
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    if (packet == null)
                    {
                        await DisconnectInternalAsync(_packetReceiverTask, null, null).ConfigureAwait(false);

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

                if (exception is AggregateException aggregateException)
                {
                    exception = aggregateException.GetBaseException();
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

                _packetDispatcher.FailAll(exception);

                await DisconnectInternalAsync(_packetReceiverTask, exception, null).ConfigureAwait(false);
            }
            finally
            {
                _logger.Verbose("Stopped receiving packets.");
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
                    var timeWithoutPacketSent = DateTime.UtcNow - _lastPacketSentTimestamp;

                    if (timeWithoutPacketSent > keepAlivePeriod)
                    {
                        using (var timeoutCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                        {
                            timeoutCancellationTokenSource.CancelAfter(Options.Timeout);
                            await PingAsync(timeoutCancellationTokenSource.Token).ConfigureAwait(false);
                        }
                    }

                    // Wait a fixed time in all cases. Calculation of the remaining time is complicated
                    // due to some edge cases and was buggy in the past. Now we wait several ms because the
                    // min keep alive value is one second so that the server will wait 1.5 seconds for a PING
                    // packet.
                    await Task.Delay(250, cancellationToken).ConfigureAwait(false);
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

                await DisconnectInternalAsync(_keepAlivePacketsSenderTask, exception, null).ConfigureAwait(false);
            }
            finally
            {
                _logger.Verbose("Stopped sending keep alive packets.");
            }
        }
    }
}