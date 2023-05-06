// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Client.Internal;
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
        readonly MqttClientAuthenticationHandler _authenticationHandler;
        readonly object _disconnectLock = new object();
        readonly MqttClientEvents _events = new MqttClientEvents();
        readonly MqttClientKeepAliveHandler _keepAliveHandler;
        readonly MqttNetSourceLogger _logger;
        readonly MqttPacketIdentifierProvider _packetIdentifierProvider = new MqttPacketIdentifierProvider();
        readonly IMqttNetLogger _rootLogger;

        IMqttChannelAdapter _adapter;

        internal bool _cleanDisconnectInitiated;

        CancellationTokenSource _clientAlive;
        volatile int _connectionStatus;

        // The value for this field can be set from two different enums.
        // They contain the same values but the set is reduced in one case.
        int _disconnectReason;
        string _disconnectReasonString;
        List<MqttUserProperty> _disconnectUserProperties;
        MqttPacketDispatcher _packetDispatcher;
        Task _packetReceiverTask;
        AsyncQueue<MqttPublishPacket> _publishPacketReceiverQueue;
        Task _publishPacketReceiverTask;

        public MqttClient(IMqttClientAdapterFactory channelFactory, IMqttNetLogger logger)
        {
            _adapterFactory = channelFactory ?? throw new ArgumentNullException(nameof(channelFactory));
            _rootLogger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger = logger.WithSource(nameof(MqttClient));

            _authenticationHandler = new MqttClientAuthenticationHandler(this, logger);
            _keepAliveHandler = new MqttClientKeepAliveHandler(this, logger);
        }

        public event Func<MqttApplicationMessageReceivedEventArgs, Task> ApplicationMessageReceivedAsync
        {
            add => _events.ApplicationMessageReceivedEvent.AddHandler(value);
            remove => _events.ApplicationMessageReceivedEvent.RemoveHandler(value);
        }

        public event Func<MqttClientConnectedEventArgs, Task> ConnectedAsync
        {
            add => _events.ConnectedEvent.AddHandler(value);
            remove => _events.ConnectedEvent.RemoveHandler(value);
        }

        public event Func<MqttClientConnectingEventArgs, Task> ConnectingAsync
        {
            add => _events.ConnectingEvent.AddHandler(value);
            remove => _events.ConnectingEvent.RemoveHandler(value);
        }

        public event Func<MqttClientDisconnectedEventArgs, Task> DisconnectedAsync
        {
            add => _events.DisconnectedEvent.AddHandler(value);
            remove => _events.DisconnectedEvent.RemoveHandler(value);
        }

        public event Func<MqttExtendedAuthenticationExchangeEventArgs, Task> ExtendedAuthenticationExchangeAsync
        {
            add => _authenticationHandler.ExtendedAuthenticationExchangeEvent.AddHandler(value);
            remove => _authenticationHandler.ExtendedAuthenticationExchangeEvent.RemoveHandler(value);
        }

        public event Func<InspectMqttPacketEventArgs, Task> InspectPacketAsync
        {
            add => _events.InspectPacketEvent.AddHandler(value);
            remove => _events.InspectPacketEvent.RemoveHandler(value);
        }

        public bool IsConnected => (MqttClientConnectionStatus)_connectionStatus == MqttClientConnectionStatus.Connected;

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

                if (_events.ConnectingEvent.HasHandlers)
                {
                    await _events.ConnectingEvent.InvokeAsync(new MqttClientConnectingEventArgs(options));
                }

                Cleanup();

                _packetIdentifierProvider.Reset();
                _packetDispatcher = new MqttPacketDispatcher();
                _clientAlive = new CancellationTokenSource();

                var adapter = _adapterFactory.CreateClientAdapter(options, new MqttPacketInspector(_events.InspectPacketEvent, _rootLogger), _rootLogger);
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

                CompareExchangeConnectionStatus(MqttClientConnectionStatus.Connected, MqttClientConnectionStatus.Connecting);

                _logger.Info("Connected");

                await OnConnected(connectResult).ConfigureAwait(false);

                return connectResult;
            }
            catch (Exception exception)
            {
                if (exception is MqttConnectingFailedException connectingFailedException)
                {
                    connectResult = connectingFailedException.Result;
                }

                _disconnectReason = (int)MqttClientDisconnectOptionsReason.UnspecifiedError;

                _logger.Error(exception, "Error while connecting with server");

                await DisconnectInternal(null, exception, connectResult).ConfigureAwait(false);

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
                    await Send(disconnectPacket, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    using (var timeout = new CancellationTokenSource(Options.Timeout))
                    {
                        await Send(disconnectPacket, timeout.Token).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                await DisconnectCore(null, null, null, clientWasConnected).ConfigureAwait(false);
            }
        }

        public async Task PingAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.CanBeCanceled)
            {
                await Request<MqttPingRespPacket>(MqttPingReqPacket.Instance, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                using (var timeout = new CancellationTokenSource(Options.Timeout))
                {
                    await Request<MqttPingRespPacket>(MqttPingReqPacket.Instance, timeout.Token).ConfigureAwait(false);
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
                    return PublishAtLeastOnce(publishPacket, cancellationToken);
                }
                case MqttQualityOfServiceLevel.ExactlyOnce:
                {
                    return PublishExactlyOnce(publishPacket, cancellationToken);
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

            return Send(authPacket, cancellationToken);
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
                subAckPacket = await Request<MqttSubAckPacket>(subscribePacket, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                using (var timeout = new CancellationTokenSource(Options.Timeout))
                {
                    subAckPacket = await Request<MqttSubAckPacket>(subscribePacket, timeout.Token).ConfigureAwait(false);
                }
            }

            return MqttClientResultFactory.SubscribeResult.Create(subscribePacket, subAckPacket);
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
                unsubAckPacket = await Request<MqttUnsubAckPacket>(unsubscribePacket, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                using (var timeout = new CancellationTokenSource(Options.Timeout))
                {
                    unsubAckPacket = await Request<MqttUnsubAckPacket>(unsubscribePacket, timeout.Token).ConfigureAwait(false);
                }
            }

            return MqttClientResultFactory.UnsubscribeResult.Create(unsubscribePacket, unsubAckPacket);
        }

        internal Task DisconnectInternalAsync(Task sender, Exception exception, MqttClientConnectResult connectResult)
        {
            var clientWasConnected = IsConnected;

            if (!DisconnectIsPendingOrFinished())
            {
                return DisconnectCore(sender, exception, connectResult, clientWasConnected);
            }

            return CompletedTask.Instance;
        }

        internal Task Send(MqttPacket packet, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _keepAliveHandler.TrackSentPacket();

            return _adapter.SendPacketAsync(packet, cancellationToken);
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
                    return Send(pubAckPacket, cancellationToken);
                }
            }
            else if (eventArgs.PublishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.ExactlyOnce)
            {
                if (!eventArgs.ProcessingFailed)
                {
                    var pubRecPacket = MqttPacketFactories.PubRec.Create(eventArgs);
                    return Send(pubRecPacket, cancellationToken);
                }
            }
            else
            {
                throw new MqttProtocolViolationException("Received a not supported QoS level.");
            }

            return CompletedTask.Instance;
        }

        void Cleanup()
        {
            try
            {
                _clientAlive?.Cancel(false);
            }
            finally
            {
                _clientAlive?.Dispose();
                _clientAlive = null;

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
            var clientAliveToken = _clientAlive.Token;

            using (var effectiveCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(clientAliveToken, cancellationToken))
            {
                _logger.Verbose("Trying to connect with server '{0}'", Options.ChannelOptions);

                await _adapter.ConnectAsync(effectiveCancellationToken.Token).ConfigureAwait(false);

                _logger.Verbose("Connection with server established");

                var connectResult = await _authenticationHandler.Authenticate(_adapter, Options, effectiveCancellationToken.Token).ConfigureAwait(false);

                if (connectResult.ResultCode == MqttClientConnectResultCode.Success)
                {
                    _publishPacketReceiverQueue?.Dispose();
                    _publishPacketReceiverQueue = new AsyncQueue<MqttPublishPacket>();

                    _publishPacketReceiverTask = Task.Run(() => ProcessReceivedPublishPacketsLoop(clientAliveToken), clientAliveToken);
                    _packetReceiverTask = Task.Run(() => ReceivePacketsLoop(clientAliveToken), clientAliveToken);

                    _keepAliveHandler.Enable(connectResult);
                }

                return connectResult;
            }
        }

        async Task DisconnectCore(Task sender, Exception exception, MqttClientConnectResult connectResult, bool clientWasConnected)
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

                _keepAliveHandler.Disable();

                await Task.WhenAll(receiverTask, publishPacketReceiverTask).ConfigureAwait(false);
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
                Task.Run(() => _events.DisconnectedEvent.InvokeAsync(eventArgs)).RunInBackground(_logger);
            }
        }

        Task DisconnectInternal(Task sender, Exception exception, MqttClientConnectResult connectResult)
        {
            var clientWasConnected = IsConnected;

            if (!DisconnectIsPendingOrFinished())
            {
                return DisconnectCore(sender, exception, connectResult, clientWasConnected);
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
            } while (true);
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

        async Task<MqttApplicationMessageReceivedEventArgs> HandleReceivedApplicationMessage(MqttPublishPacket publishPacket)
        {
            var applicationMessage = MqttApplicationMessageFactory.Create(publishPacket);
            var eventArgs = new MqttApplicationMessageReceivedEventArgs(Options.ClientId, applicationMessage, publishPacket, AcknowledgeReceivedPublishPacket);
            await _events.ApplicationMessageReceivedEvent.InvokeAsync(eventArgs).ConfigureAwait(false);

            return eventArgs;
        }

        Task OnConnected(MqttClientConnectResult connectResult)
        {
            if (_events.ConnectedEvent.HasHandlers)
            {
                var eventArgs = new MqttClientConnectedEventArgs(connectResult);
                return _events.ConnectedEvent.InvokeAsync(eventArgs);
            }

            return CompletedTask.Instance;
        }

        Task ProcessReceivedAuthPacket(MqttAuthPacket authPacket)
        {
            var extendedAuthenticationExchangeHandler = Options.ExtendedAuthenticationExchangeHandler;
            if (extendedAuthenticationExchangeHandler != null)
            {
                return extendedAuthenticationExchangeHandler.HandleRequestAsync(new MqttExtendedAuthenticationExchangeEventArgs(authPacket));
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

            return DisconnectInternal(_packetReceiverTask, null, null);
        }

        async Task ProcessReceivedPublishPacketsLoop(CancellationToken cancellationToken)
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
                    var eventArgs = await HandleReceivedApplicationMessage(publishPacket).ConfigureAwait(false);

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
                return Send(pubRelPacket, cancellationToken);
            }

            return CompletedTask.Instance;
        }

        Task ProcessReceivedPubRelPacket(MqttPubRelPacket pubRelPacket, CancellationToken cancellationToken)
        {
            var pubCompPacket = MqttPacketFactories.PubComp.Create(pubRelPacket, MqttApplicationMessageReceivedReasonCode.Success);
            return Send(pubCompPacket, cancellationToken);
        }

        async Task<MqttClientPublishResult> PublishAtLeastOnce(MqttPublishPacket publishPacket, CancellationToken cancellationToken)
        {
            publishPacket.PacketIdentifier = _packetIdentifierProvider.GetNextPacketIdentifier();

            var pubAckPacket = await Request<MqttPubAckPacket>(publishPacket, cancellationToken).ConfigureAwait(false);
            return MqttClientResultFactory.PublishResult.Create(pubAckPacket);
        }

        async Task<MqttClientPublishResult> PublishAtMostOnce(MqttPublishPacket publishPacket, CancellationToken cancellationToken)
        {
            // No packet identifier is used for QoS 0 [3.3.2.2 Packet Identifier]
            await Send(publishPacket, cancellationToken).ConfigureAwait(false);

            return MqttClientResultFactory.PublishResult.Create(null);
        }

        async Task<MqttClientPublishResult> PublishExactlyOnce(MqttPublishPacket publishPacket, CancellationToken cancellationToken)
        {
            publishPacket.PacketIdentifier = _packetIdentifierProvider.GetNextPacketIdentifier();

            var pubRecPacket = await Request<MqttPubRecPacket>(publishPacket, cancellationToken).ConfigureAwait(false);

            var pubRelPacket = MqttPacketFactories.PubRel.Create(pubRecPacket, MqttApplicationMessageReceivedReasonCode.Success);

            var pubCompPacket = await Request<MqttPubCompPacket>(pubRelPacket, cancellationToken).ConfigureAwait(false);

            return MqttClientResultFactory.PublishResult.Create(pubRecPacket, pubCompPacket);
        }

        async Task<MqttPacket> Receive(CancellationToken cancellationToken)
        {
            var packetTask = _adapter.ReceivePacketAsync(cancellationToken);

            MqttPacket packet;
            if (packetTask.IsCompleted)
            {
                packet = packetTask.Result;
            }
            else
            {
                packet = await packetTask.ConfigureAwait(false);
            }

            return packet;
        }

        async Task ReceivePacketsLoop(CancellationToken cancellationToken)
        {
            try
            {
                _logger.Verbose("Start receiving packets.");

                while (!cancellationToken.IsCancellationRequested)
                {
                    var packet = await Receive(cancellationToken).ConfigureAwait(false);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    if (packet == null)
                    {
                        await DisconnectInternal(_packetReceiverTask, null, null).ConfigureAwait(false);

                        return;
                    }

                    await TryProcessReceivedPacket(packet, cancellationToken).ConfigureAwait(false);
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

                await DisconnectInternal(_packetReceiverTask, exception, null).ConfigureAwait(false);
            }
            finally
            {
                _logger.Verbose("Stopped receiving packets.");
            }
        }

        async Task<TResponsePacket> Request<TResponsePacket>(MqttPacket requestPacket, CancellationToken cancellationToken) where TResponsePacket : MqttPacket
        {
            cancellationToken.ThrowIfCancellationRequested();

            ushort packetIdentifier = 0;
            if (requestPacket is MqttPacketWithIdentifier packetWithIdentifier)
            {
                packetIdentifier = packetWithIdentifier.PacketIdentifier;
            }

            using (var packetAwaitable = _packetDispatcher.AddAwaitable<TResponsePacket>(packetIdentifier))
            {
                try
                {
                    await Send(requestPacket, cancellationToken).ConfigureAwait(false);
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

        void ThrowIfConnected(string message)
        {
            if (IsConnected)
            {
                throw new InvalidOperationException(message);
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
                    _clientAlive?.Cancel(false);
                }
                catch (Exception exception)
                {
                    _logger.Warning(exception, "Error while initiating disconnect.");
                }
            }
        }

        async Task TryProcessReceivedPacket(MqttPacket packet, CancellationToken cancellationToken)
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

                await DisconnectInternal(_packetReceiverTask, exception, null).ConfigureAwait(false);
            }
        }
    }
}