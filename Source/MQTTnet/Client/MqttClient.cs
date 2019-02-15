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

        private CancellationTokenSource _backgroundCancellationTokenSource;
        private Task _packetReceiverTask;
        private Task _keepAlivePacketsSenderTask;

        private IMqttChannelAdapter _adapter;
        private bool _cleanDisconnectInitiated;
        private long _disconnectGate;

        public MqttClient(IMqttClientAdapterFactory channelFactory, IMqttNetLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            _adapterFactory = channelFactory ?? throw new ArgumentNullException(nameof(channelFactory));
            _logger = logger.CreateChildLogger(nameof(MqttClient));
        }

        public IMqttClientConnectedHandler ConnectedHandler { get; set; }

        public event EventHandler<MqttClientConnectedEventArgs> Connected;

        public IMqttClientDisconnectedHandler DisconnectedHandler { get; set; }

        public event EventHandler<MqttClientDisconnectedEventArgs> Disconnected;

        public IMqttApplicationMessageHandler ReceivedApplicationMessageHandler { get; set; }

        public event EventHandler<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;

        public bool IsConnected { get; private set; }

        public IMqttClientOptions Options { get; private set; }

        public async Task<MqttClientAuthenticateResult> ConnectAsync(IMqttClientOptions options, CancellationToken cancellationToken)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (options.ChannelOptions == null) throw new ArgumentException("ChannelOptions are not set.");

            ThrowIfConnected("It is not allowed to connect with a server after the connection is established.");

            try
            {
                Options = options;

                _packetIdentifierProvider.Reset();
                _packetDispatcher.Reset();

                _backgroundCancellationTokenSource = new CancellationTokenSource();
                var backgroundCancellationToken = _backgroundCancellationTokenSource.Token;

                _disconnectGate = 0;
                var adapter = _adapterFactory.CreateClientAdapter(options, _logger);
                _adapter = adapter;

                _logger.Verbose($"Trying to connect with server '{options.ChannelOptions}' (Timeout={options.CommunicationTimeout}).");
                await _adapter.ConnectAsync(options.CommunicationTimeout, cancellationToken).ConfigureAwait(false);
                _logger.Verbose("Connection with server established.");

                _packetReceiverTask = Task.Run(() => TryReceivePacketsAsync(backgroundCancellationToken), backgroundCancellationToken);

                var authenticateResult = await AuthenticateAsync(adapter, options.WillMessage, cancellationToken).ConfigureAwait(false);

                _sendTracker.Restart();

                if (Options.KeepAlivePeriod != TimeSpan.Zero)
                {
                    _keepAlivePacketsSenderTask = Task.Run(() => TrySendKeepAliveMessagesAsync(backgroundCancellationToken), backgroundCancellationToken);
                }

                IsConnected = true;

                _logger.Info("Connected.");

                Connected?.Invoke(this, new MqttClientConnectedEventArgs(authenticateResult));
                await ConnectedHandler?.HandleConnectedAsync(new MqttClientConnectedEventArgs(authenticateResult));

                return authenticateResult;
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

        public async Task DisconnectAsync(MqttClientDisconnectOptions options, CancellationToken cancellationToken)
        {
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
                    await DisconnectInternalAsync(null, null).ConfigureAwait(false);
                }
            }
        }

        public async Task<MqttClientSubscribeResult> SubscribeAsync(MqttClientSubscribeOptions options, CancellationToken cancellationToken)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            ThrowIfNotConnected();

            var subscribePacket = _adapter.PacketFormatterAdapter.DataConverter.CreateSubscribePacket(options);
            subscribePacket.PacketIdentifier = _packetIdentifierProvider.GetNextPacketIdentifier();

            var subAckPacket = await SendAndReceiveAsync<MqttSubAckPacket>(subscribePacket, cancellationToken).ConfigureAwait(false);
            return _adapter.PacketFormatterAdapter.DataConverter.CreateClientSubscribeResult(subscribePacket, subAckPacket);
        }

        public async Task<MqttClientUnsubscribeResult> UnsubscribeAsync(MqttClientUnsubscribeOptions options, CancellationToken cancellationToken)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            ThrowIfNotConnected();

            var unsubscribePacket = _adapter.PacketFormatterAdapter.DataConverter.CreateUnsubscribePacket(options);
            unsubscribePacket.PacketIdentifier = _packetIdentifierProvider.GetNextPacketIdentifier();

            var unsubAckPacket = await SendAndReceiveAsync<MqttUnsubAckPacket>(unsubscribePacket, cancellationToken).ConfigureAwait(false);
            return _adapter.PacketFormatterAdapter.DataConverter.CreateClientUnsubscribeResult(unsubscribePacket, unsubAckPacket);
        }

        public Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));

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

        public void Dispose()
        {
            _backgroundCancellationTokenSource?.Cancel(false);
            _backgroundCancellationTokenSource?.Dispose();
            _backgroundCancellationTokenSource = null;

            _adapter?.Dispose();
            _adapter = null;
        }

        private async Task<MqttClientAuthenticateResult> AuthenticateAsync(IMqttChannelAdapter channelAdapter, MqttApplicationMessage willApplicationMessage, CancellationToken cancellationToken)
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

            _logger.Verbose("Authenticated MQTT connection with server established.");

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
                    _logger.Verbose("Disconnecting [Timeout={0}]", Options.CommunicationTimeout);
                    await _adapter.DisconnectAsync(Options.CommunicationTimeout, CancellationToken.None).ConfigureAwait(false);
                }

                await WaitForTaskAsync(_packetReceiverTask, sender).ConfigureAwait(false);
                await WaitForTaskAsync(_keepAlivePacketsSenderTask, sender).ConfigureAwait(false);

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
                await DisconnectedHandler?.HandleDisconnectedAsync(new MqttClientDisconnectedEventArgs(clientWasConnected, exception));
            }
        }

        private void InitiateDisconnect()
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
                    await _adapter.SendPacketAsync(requestPacket, Options.CommunicationTimeout, cancellationToken).ConfigureAwait(false);
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

                    if (packet == null)
                    {
                        if (!DisconnectIsPending())
                        {
                            await DisconnectInternalAsync(_packetReceiverTask, null).ConfigureAwait(false);
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
                    await DisconnectAsync(null, cancellationToken).ConfigureAwait(false);
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
                    await HandleReceivedApplicationMessageAsync(publishPacket).ConfigureAwait(false);

                    var pubRecPacket = new MqttPubRecPacket
                    {
                        PacketIdentifier = publishPacket.PacketIdentifier,
                        ReasonCode = MqttPubRecReasonCode.Success
                    };

                    await SendAsync(pubRecPacket, cancellationToken).ConfigureAwait(false);
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

        private async Task<MqttClientPublishResult> PublishAtMostOnce(MqttPublishPacket publishPacket, CancellationToken cancellationToken)
        {
            // No packet identifier is used for QoS 0 [3.3.2.2 Packet Identifier]
            await SendAsync(publishPacket, cancellationToken).ConfigureAwait(false);
            return _adapter.PacketFormatterAdapter.DataConverter.CreatePublishResult(null);
        }

        private async Task<MqttClientPublishResult> PublishAtLeastOnceAsync(MqttPublishPacket publishPacket, CancellationToken cancellationToken)
        {
            publishPacket.PacketIdentifier = _packetIdentifierProvider.GetNextPacketIdentifier();
            var response = await SendAndReceiveAsync<MqttPubAckPacket>(publishPacket, cancellationToken).ConfigureAwait(false);
            return _adapter.PacketFormatterAdapter.DataConverter.CreatePublishResult(response);
        }

        private async Task<MqttClientPublishResult> PublishExactlyOnceAsync(MqttPublishPacket publishPacket, CancellationToken cancellationToken)
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
    }
}
