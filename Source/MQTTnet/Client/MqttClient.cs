using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Receiving;
using MQTTnet.Client.Subscribing;
using MQTTnet.Client.Unsubscribing;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.PacketDispatcher;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Client
{
    public class MqttClient : IMqttClient
    {
        private readonly MqttPacketIdentifierProvider _packetIdentifierProvider = new MqttPacketIdentifierProvider();
        private readonly MqttPacketDispatcher _packetDispatcher = new MqttPacketDispatcher();
        private readonly Stopwatch _sendTracker = new Stopwatch();
        private readonly object _disconnectLock = new object();

        private readonly IMqttClientAdapterFactory _adapterFactory;
        private readonly IMqttNetChildLogger _logger;

        private CancellationTokenSource _cancellationTokenSource;
        private Task _packetReceiverTask;
        private Task _keepAlivePacketsSenderTask;
        private Task _backgroundWorkerTask;

        private IMqttChannelAdapter _adapter;
        private bool _cleanDisconnectInitiated;
        private long _disconnectGate;

        public MqttClient(IMqttClientAdapterFactory channelFactory, IMqttNetLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            _adapterFactory = channelFactory ?? throw new ArgumentNullException(nameof(channelFactory));
            _logger = logger.CreateChildLogger(nameof(MqttClient));
        }

        public event EventHandler<MqttClientConnectedEventArgs> Connected;
        public event EventHandler<MqttClientDisconnectedEventArgs> Disconnected;

        public IMqttApplicationMessageHandler ReceivedApplicationMessageHandler { get; set; }
        public event EventHandler<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;

        public bool IsConnected { get; private set; }
        public IMqttClientOptions Options { get; private set; }

        public async Task<MqttClientConnectResult> ConnectAsync(IMqttClientOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (options.ChannelOptions == null) throw new ArgumentException("ChannelOptions are not set.");

            ThrowIfConnected("It is not allowed to connect with a server after the connection is established.");

            try
            {
                Options = options;

                _packetIdentifierProvider.Reset();
                _packetDispatcher.Reset();

                _cancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = _cancellationTokenSource.Token;

                _disconnectGate = 0;
                var adapter = _adapterFactory.CreateClientAdapter(options, _logger);
                _adapter = adapter;

                _logger.Verbose($"Trying to connect with server ({Options.ChannelOptions}).");
                await _adapter.ConnectAsync(Options.CommunicationTimeout, cancellationToken).ConfigureAwait(false);
                _logger.Verbose("Connection with server established.");

                _packetReceiverTask = Task.Run(() => TryReceivePacketsAsync(cancellationToken), cancellationToken);

                var connectResult = await AuthenticateAsync(adapter, options.WillMessage, cancellationToken).ConfigureAwait(false);
                _logger.Verbose("MQTT connection with server established.");

                _sendTracker.Restart();

                if (Options.KeepAlivePeriod != TimeSpan.Zero)
                {
                    _keepAlivePacketsSenderTask = Task.Run(() => TrySendKeepAliveMessagesAsync(cancellationToken), cancellationToken);
                }

                _backgroundWorkerTask = Task.Run(() => TryProcessReceivedPacketsAsync(cancellationToken), cancellationToken);

                IsConnected = true;
                Connected?.Invoke(this, new MqttClientConnectedEventArgs(connectResult));

                _logger.Info("Connected.");

                return connectResult;
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Error while connecting with server.");

                if (!DisconnectIsPending())
                {
                    await DisconnectInternalAsync(null, exception).ConfigureAwait(false);
                }

                throw;
            }
        }

        public async Task DisconnectAsync(MqttClientDisconnectOptions options)
        {
            try
            {
                _cleanDisconnectInitiated = true;

                if (IsConnected && _cancellationTokenSource?.IsCancellationRequested == false)
                {
                    var disconnectPacket = CreateDisconnectPacket(options);
                    await SendAsync(disconnectPacket, _cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }
            finally
            {
                if (!DisconnectIsPending())
                {
                    await DisconnectInternalAsync(null, null).ConfigureAwait(false);
                }
            }
        }

        public async Task<MqttClientSubscribeResult> SubscribeAsync(MqttClientSubscribeOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            ThrowIfNotConnected();

            var subscribePacket = _adapter.PacketFormatterAdapter.DataConverter.CreateSubscribePacket(options);
            subscribePacket.PacketIdentifier = _packetIdentifierProvider.GetNextPacketIdentifier();

            var subAckPacket = await SendAndReceiveAsync<MqttSubAckPacket>(subscribePacket, _cancellationTokenSource.Token).ConfigureAwait(false);
            return _adapter.PacketFormatterAdapter.DataConverter.CreateClientSubscribeResult(subscribePacket, subAckPacket);
        }

        public async Task<MqttClientUnsubscribeResult> UnsubscribeAsync(MqttClientUnsubscribeOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            ThrowIfNotConnected();

            var unsubscribePacket = _adapter.PacketFormatterAdapter.DataConverter.CreateUnsubscribePacket(options);
            unsubscribePacket.PacketIdentifier = _packetIdentifierProvider.GetNextPacketIdentifier();

            var unsubAckPacket = await SendAndReceiveAsync<MqttUnsubAckPacket>(unsubscribePacket, _cancellationTokenSource.Token).ConfigureAwait(false);
            return _adapter.PacketFormatterAdapter.DataConverter.CreateClientUnsubscribeResult(unsubscribePacket, unsubAckPacket);
        }

        public async Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

            ThrowIfNotConnected();

            var publishPacket = _adapter.PacketFormatterAdapter.DataConverter.CreatePublishPacket(applicationMessage);

            switch (applicationMessage.QualityOfServiceLevel)
            {
                case MqttQualityOfServiceLevel.AtMostOnce:
                    {
                        // No packet identifier is used for QoS 0 [3.3.2.2 Packet Identifier]
                        await SendAsync(publishPacket, _cancellationTokenSource.Token).ConfigureAwait(false);
                        return new MqttClientPublishResult();
                    }
                case MqttQualityOfServiceLevel.AtLeastOnce:
                    {
                        publishPacket.PacketIdentifier = _packetIdentifierProvider.GetNextPacketIdentifier();
                        var response = await SendAndReceiveAsync<MqttPubAckPacket>(publishPacket, _cancellationTokenSource.Token).ConfigureAwait(false);

                        var result = new MqttClientPublishResult();
                        if (response.ReasonCode != null)
                        {
                            result.ReasonCode = (MqttClientPublishReasonCode)response.ReasonCode;
                        }

                        return result;
                    }
                case MqttQualityOfServiceLevel.ExactlyOnce:
                    {
                        return await PublishExactlyOnceAsync(publishPacket, _cancellationTokenSource.Token).ConfigureAwait(false);
                    }
                default:
                    {
                        throw new NotSupportedException();
                    }
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel(false);
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            _adapter?.Dispose();
            _adapter = null;
        }

        private async Task<MqttClientConnectResult> AuthenticateAsync(IMqttChannelAdapter channelAdapter, MqttApplicationMessage willApplicationMessage, CancellationToken cancellationToken)
        {
            var connectPacket = channelAdapter.PacketFormatterAdapter.DataConverter.CreateConnectPacket(
                willApplicationMessage,
                Options);

            var connAckPacket = await SendAndReceiveAsync<MqttConnAckPacket>(connectPacket, cancellationToken).ConfigureAwait(false);
            var result = channelAdapter.PacketFormatterAdapter.DataConverter.CreateClientConnectResult(connAckPacket);

            if (result.ResultCode != MqttClientConnectResultCode.Success)
            {
                throw new MqttConnectingFailedException(result.ResultCode);
            }

            return result;
        }

        private void ThrowIfNotConnected()
        {
            if (!IsConnected || Interlocked.Read(ref _disconnectGate) == 1) throw new MqttCommunicationException("The client is not connected.");
        }

        private void ThrowIfConnected(string message)
        {
            if (IsConnected) throw new MqttProtocolViolationException(message);
        }

        private async Task DisconnectInternalAsync(Task sender, Exception exception)
        {
            var clientWasConnected = IsConnected;

            InitiateDisconnect();

            IsConnected = false;

            try
            {
                if (_adapter != null)
                {
                    await _adapter.DisconnectAsync(Options.CommunicationTimeout, CancellationToken.None).ConfigureAwait(false);
                }

                await WaitForTaskAsync(_packetReceiverTask, sender).ConfigureAwait(false);
                await WaitForTaskAsync(_keepAlivePacketsSenderTask, sender).ConfigureAwait(false);
                await WaitForTaskAsync(_backgroundWorkerTask, sender).ConfigureAwait(false);

                _logger.Verbose("Disconnected from adapter.");
            }
            catch (Exception adapterException)
            {
                _logger.Warning(adapterException, "Error while disconnecting from adapter.");
            }
            finally
            {
                Dispose();
                _cleanDisconnectInitiated = false;

                _logger.Info("Disconnected.");
                Disconnected?.Invoke(this, new MqttClientDisconnectedEventArgs(clientWasConnected, exception));
            }
        }

        private void InitiateDisconnect()
        {
            lock (_disconnectLock)
            {
                try
                {
                    if (_cancellationTokenSource?.IsCancellationRequested == true)
                    {
                        return;
                    }

                    _cancellationTokenSource?.Cancel(false);
                }
                catch (Exception exception)
                {
                    _logger.Warning(exception, "Error while initiating disconnect.");
                }
            }
        }

        private Task SendAsync(MqttBasePacket packet, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _sendTracker.Restart();

            return _adapter.SendPacketAsync(packet, cancellationToken);
        }

        private async Task<TResponsePacket> SendAndReceiveAsync<TResponsePacket>(MqttBasePacket requestPacket, CancellationToken cancellationToken) where TResponsePacket : MqttBasePacket
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
                    await _adapter.SendPacketAsync(requestPacket, cancellationToken).ConfigureAwait(false);
                    return await packetAwaiter.WaitOneAsync(Options.CommunicationTimeout).ConfigureAwait(false);
                }
                catch (MqttCommunicationTimedOutException)
                {
                    _logger.Warning(null, "Timeout while waiting for packet of type '{0}'.", typeof(TResponsePacket).Namespace);
                    throw;
                }
            }
        }

        private async Task TrySendKeepAliveMessagesAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.Verbose("Start sending keep alive packets.");

                while (!cancellationToken.IsCancellationRequested)
                {
                    var keepAliveSendInterval = TimeSpan.FromSeconds(Options.KeepAlivePeriod.TotalSeconds * 0.75);
                    if (Options.KeepAliveSendInterval.HasValue)
                    {
                        keepAliveSendInterval = Options.KeepAliveSendInterval.Value;
                    }

                    var waitTime = keepAliveSendInterval - _sendTracker.Elapsed;
                    if (waitTime <= TimeSpan.Zero)
                    {
                        await SendAndReceiveAsync<MqttPingRespPacket>(new MqttPingReqPacket(), cancellationToken).ConfigureAwait(false);
                        waitTime = keepAliveSendInterval;
                    }

                    await Task.Delay(waitTime, cancellationToken).ConfigureAwait(false);
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
                    _logger.Warning(exception, "MQTT communication exception while sending/receiving keep alive packets.");
                }
                else
                {
                    _logger.Error(exception, "Unhandled exception while sending/receiving keep alive packets.");
                }

                if (!DisconnectIsPending())
                {
                    await DisconnectInternalAsync(_keepAlivePacketsSenderTask, exception).ConfigureAwait(false);
                }
            }
            finally
            {
                _logger.Verbose("Stopped sending keep alive packets.");
            }
        }

        private async Task TryReceivePacketsAsync(CancellationToken cancellationToken)
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

                    if (packet == null && !DisconnectIsPending())
                    {
                        await DisconnectInternalAsync(_packetReceiverTask, null).ConfigureAwait(false);
                    }

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
                    _logger.Warning(exception, "MQTT communication exception while receiving packets.");
                }
                else
                {
                    _logger.Error(exception, "Unhandled exception while receiving packets.");
                }

                _packetDispatcher.Dispatch(exception);

                if (!DisconnectIsPending())
                {
                    await DisconnectInternalAsync(_packetReceiverTask, exception).ConfigureAwait(false);
                }
            }
            finally
            {
                _logger.Verbose("Stopped receiving packets.");
            }
        }

        private async Task TryProcessReceivedPacketAsync(MqttBasePacket packet, CancellationToken cancellationToken)
        {
            try
            {
                if (packet is MqttPublishPacket publishPacket)
                {
                    await TryProcessReceivedPublishPacketAsync(publishPacket, cancellationToken).ConfigureAwait(false);
                }
                else if (packet is MqttPingReqPacket)
                {
                    await SendAsync(new MqttPingRespPacket(), cancellationToken).ConfigureAwait(false);
                }
                else if (packet is MqttDisconnectPacket)
                {
                    await DisconnectAsync(null).ConfigureAwait(false);
                }
                else
                {
                    throw new MqttProtocolViolationException($"Received packet '{packet}' at an unexpected time.");
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
                    _logger.Warning(exception, "MQTT communication exception while receiving packets.");
                }
                else
                {
                    _logger.Error(exception, "Unhandled exception while receiving packets.");
                }

                _packetDispatcher.Dispatch(exception);

                if (!DisconnectIsPending())
                {
                    await DisconnectInternalAsync(_packetReceiverTask, exception).ConfigureAwait(false);
                }
            }
        }

        private async Task TryProcessReceivedPublishPacketAsync(MqttPublishPacket publishPacket, CancellationToken cancellationToken)
        {
            try
            {
                if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtMostOnce)
                {
                    await HandleReceivedApplicationMessageAsync(publishPacket).ConfigureAwait(false);
                }
                else if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtLeastOnce)
                {
                    await HandleReceivedApplicationMessageAsync(publishPacket).ConfigureAwait(false);

                    await SendAsync(new MqttPubAckPacket
                    {
                        PacketIdentifier = publishPacket.PacketIdentifier,
                        ReasonCode = MqttPubAckReasonCode.Success
                    }, cancellationToken).ConfigureAwait(false);
                }
                else if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.ExactlyOnce)
                {
                    var pubRecPacket = new MqttPubRecPacket
                    {
                        PacketIdentifier = publishPacket.PacketIdentifier,
                        ReasonCode = MqttPubRecReasonCode.Success
                    };

                    var pubRelPacket = await SendAndReceiveAsync<MqttPubRelPacket>(pubRecPacket, cancellationToken).ConfigureAwait(false);

                    // QoS 2 is implement as method "B" (4.3.3 QoS 2: Exactly once delivery)
                    await HandleReceivedApplicationMessageAsync(publishPacket).ConfigureAwait(false);

                    await SendAsync(new MqttPubCompPacket
                    {
                        PacketIdentifier = pubRelPacket.PacketIdentifier,
                        ReasonCode = MqttPubCompReasonCode.Success
                    }, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    throw new MqttProtocolViolationException("Received a not supported QoS level.");
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Unhandled exception while handling application message.");
            }
        }

        private async Task TryProcessReceivedPacketsAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var packet = _packetDispatcher.Take(cancellationToken);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    await TryProcessReceivedPacketAsync(packet, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Error while processing packet.");
            }
        }

        private async Task<MqttClientPublishResult> PublishExactlyOnceAsync(MqttPublishPacket publishPacket, CancellationToken cancellationToken)
        {
            publishPacket.PacketIdentifier = _packetIdentifierProvider.GetNextPacketIdentifier();

            var pubRecPacket = await SendAndReceiveAsync<MqttPubRecPacket>(publishPacket, cancellationToken).ConfigureAwait(false);

            // TODO: Check response code.

            var pubRelPacket = new MqttPubRelPacket
            {
                PacketIdentifier = publishPacket.PacketIdentifier,
                ReasonCode = MqttPubRelReasonCode.Success
            };

            var pubCompPacket = await SendAndReceiveAsync<MqttPubCompPacket>(pubRelPacket, cancellationToken).ConfigureAwait(false);

            // TODO: Check response code.

            var result = new MqttClientPublishResult();

            if (pubRecPacket.ReasonCode != null)
            {
                result.ReasonCode = (MqttClientPublishReasonCode)pubRecPacket.ReasonCode;
            }

            return result;
        }

        ////private void StartReceivingPackets(CancellationToken cancellationToken)
        ////{
        ////    _packetReceiverTask = Task.Factory.StartNew(
        ////        () => TryReceivePacketsAsync(cancellationToken),
        ////        cancellationToken,
        ////        TaskCreationOptions.LongRunning,
        ////        TaskScheduler.Default).Unwrap();
        ////}

        ////private void StartSendingKeepAliveMessages(CancellationToken cancellationToken)
        ////{
        ////    _keepAlivePacketsSenderTask = Task.Factory.StartNew(
        ////        () => TrySendKeepAliveMessagesAsync(cancellationToken),
        ////        cancellationToken,
        ////        TaskCreationOptions.LongRunning,
        ////        TaskScheduler.Default).Unwrap();
        ////}

        private Task HandleReceivedApplicationMessageAsync(MqttPublishPacket publishPacket)
        {
            var applicationMessage = _adapter.PacketFormatterAdapter.DataConverter.CreateApplicationMessage(publishPacket);

            ApplicationMessageReceived?.Invoke(this, new MqttApplicationMessageReceivedEventArgs(Options.ClientId, applicationMessage));

            var handler = ReceivedApplicationMessageHandler;
            if (handler != null)
            {
                return handler.HandleApplicationMessageAsync(
                    new MqttApplicationMessageHandlerContext(Options.ClientId, applicationMessage));
            }

            return Task.FromResult(0);
        }

        private static async Task WaitForTaskAsync(Task task, Task sender)
        {
            if (task == sender || task == null)
            {
                return;
            }

            if (task.IsCanceled || task.IsCompleted || task.IsFaulted)
            {
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

        private bool DisconnectIsPending()
        {
            return Interlocked.CompareExchange(ref _disconnectGate, 1, 0) != 0;
        }

        private MqttDisconnectPacket CreateDisconnectPacket(MqttClientDisconnectOptions options)
        {
            var packet = new MqttDisconnectPacket();

            if (_adapter.PacketFormatterAdapter.ProtocolVersion == MqttProtocolVersion.V500)
            {
                if (options == null)
                {
                    packet.ReasonCode = MqttDisconnectReasonCode.NormalDisconnection;
                }
                else
                {
                    packet.ReasonCode = (MqttDisconnectReasonCode)options.ReasonCode;
                }
            }
            else
            {
                if (options != null)
                {
                    throw new MqttProtocolViolationException("Reason codes for disconnect are only supported for MQTTv5.");
                }
            }

            return packet;
        }
    }
}
