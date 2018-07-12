using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Client
{
    public class MqttClient : IMqttClient
    {
        private readonly MqttPacketIdentifierProvider _packetIdentifierProvider = new MqttPacketIdentifierProvider();
        private readonly Stopwatch _sendTracker = new Stopwatch();
        private readonly object _disconnectLock = new object();
        private readonly MqttPacketDispatcher _packetDispatcher = new MqttPacketDispatcher();

        private readonly IMqttClientAdapterFactory _adapterFactory;
        private readonly IMqttNetChildLogger _logger;

        private IMqttClientOptions _options;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _disconnectCleanerTask;
        private Task _packetReceiverTask;
        private Task _keepAliveMessageSenderTask;
        private IMqttChannelAdapter _adapter;
        private bool _cleanDisconnectInitiated;
        private Exception _disconnectException;

        public MqttClient(IMqttClientAdapterFactory channelFactory, IMqttNetLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            _adapterFactory = channelFactory ?? throw new ArgumentNullException(nameof(channelFactory));
            _logger = logger.CreateChildLogger(nameof(MqttClient));
        }

        public event EventHandler<MqttClientConnectedEventArgs> Connected;
        public event EventHandler<MqttClientDisconnectedEventArgs> Disconnected;
        public event EventHandler<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;

        public bool IsConnected { get; private set; }

        public async Task<MqttClientConnectResult> ConnectAsync(IMqttClientOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (options.ChannelOptions == null) throw new ArgumentException("ChannelOptions are not set.");

            ThrowIfConnected("It is not allowed to connect with a server after the connection is established.");

            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _options = options;
                _packetIdentifierProvider.Reset();
                _packetDispatcher.Reset();

                StartDisconnectCleaner(_cancellationTokenSource.Token);

                _adapter = _adapterFactory.CreateClientAdapter(options, _logger);

                _logger.Verbose($"Trying to connect with server ({_options.ChannelOptions}).");
                await _adapter.ConnectAsync(_options.CommunicationTimeout, _cancellationTokenSource.Token).ConfigureAwait(false);
                _logger.Verbose("Connection with server established.");

                StartReceivingPackets(_cancellationTokenSource.Token);

                var connectResponse = await AuthenticateAsync(options.WillMessage, _cancellationTokenSource.Token).ConfigureAwait(false);
                _logger.Verbose("MQTT connection with server established.");

                _sendTracker.Restart();

                if (_options.KeepAlivePeriod != TimeSpan.Zero)
                {
                    StartSendingKeepAliveMessages(_cancellationTokenSource.Token);
                }

                IsConnected = true;
                Connected?.Invoke(this, new MqttClientConnectedEventArgs(connectResponse.IsSessionPresent));

                _logger.Info("Connected.");
                return new MqttClientConnectResult(connectResponse.IsSessionPresent);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Error while connecting with server.");
                InitiateDisconnect(exception);

                await _disconnectCleanerTask.ConfigureAwait(false);

                throw;
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                _cleanDisconnectInitiated = true;

                if (IsConnected && !_cancellationTokenSource.IsCancellationRequested)
                {
                    await SendAsync(new MqttDisconnectPacket(), _cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }
            finally
            {
                InitiateDisconnect(null);

                await _disconnectCleanerTask.ConfigureAwait(false);
            }
        }

        public async Task<IList<MqttSubscribeResult>> SubscribeAsync(IEnumerable<TopicFilter> topicFilters)
        {
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            ThrowIfNotConnected();

            var subscribePacket = new MqttSubscribePacket
            {
                PacketIdentifier = _packetIdentifierProvider.GetNewPacketIdentifier(),
                TopicFilters = topicFilters.ToList()
            };

            var response = await SendAndReceiveAsync<MqttSubAckPacket>(subscribePacket, _cancellationTokenSource.Token).ConfigureAwait(false);

            if (response.SubscribeReturnCodes.Count != subscribePacket.TopicFilters.Count)
            {
                throw new MqttProtocolViolationException("The return codes are not matching the topic filters [MQTT-3.9.3-1].");
            }

            return subscribePacket.TopicFilters.Select((t, i) => new MqttSubscribeResult(t, response.SubscribeReturnCodes[i])).ToList();
        }

        public async Task UnsubscribeAsync(IEnumerable<string> topicFilters)
        {
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            ThrowIfNotConnected();

            var unsubscribePacket = new MqttUnsubscribePacket
            {
                PacketIdentifier = _packetIdentifierProvider.GetNewPacketIdentifier(),
                TopicFilters = topicFilters.ToList()
            };

            await SendAndReceiveAsync<MqttUnsubAckPacket>(unsubscribePacket, _cancellationTokenSource.Token).ConfigureAwait(false);
        }

        public Task PublishAsync(MqttApplicationMessage applicationMessage)
        {
            ThrowIfNotConnected();

            var publishPacket = applicationMessage.ToPublishPacket();

            switch (applicationMessage.QualityOfServiceLevel)
            {
                case MqttQualityOfServiceLevel.AtMostOnce:
                    {
                        // No packet identifier is used for QoS 0 [3.3.2.2 Packet Identifier]
                        return SendAsync(publishPacket, _cancellationTokenSource.Token);
                    }
                case MqttQualityOfServiceLevel.AtLeastOnce:
                    {
                        publishPacket.PacketIdentifier = _packetIdentifierProvider.GetNewPacketIdentifier();
                        return SendAndReceiveAsync<MqttPubAckPacket>(publishPacket, _cancellationTokenSource.Token);
                    }
                case MqttQualityOfServiceLevel.ExactlyOnce:
                    {
                        return PublishExactlyOnce(publishPacket, _cancellationTokenSource.Token);
                    }
                default:
                    {
                        throw new NotSupportedException();
                    }
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            _adapter?.Dispose();
        }

        private async Task<MqttConnAckPacket> AuthenticateAsync(MqttApplicationMessage willApplicationMessage, CancellationToken cancellationToken)
        {
            var connectPacket = new MqttConnectPacket
            {
                ClientId = _options.ClientId,
                Username = _options.Credentials?.Username,
                Password = _options.Credentials?.Password,
                CleanSession = _options.CleanSession,
                KeepAlivePeriod = (ushort)_options.KeepAlivePeriod.TotalSeconds,
                WillMessage = willApplicationMessage
            };

            var response = await SendAndReceiveAsync<MqttConnAckPacket>(connectPacket, cancellationToken).ConfigureAwait(false);
            if (response.ConnectReturnCode != MqttConnectReturnCode.ConnectionAccepted)
            {
                throw new MqttConnectingFailedException(response.ConnectReturnCode);
            }

            return response;
        }

        private void ThrowIfNotConnected()
        {
            if (!IsConnected) throw new MqttCommunicationException("The client is not connected.");
        }

        private void ThrowIfConnected(string message)
        {
            if (IsConnected) throw new MqttProtocolViolationException(message);
        }

        private async Task DisconnectInternalAsync()
        {
            var clientWasConnected = IsConnected;
            IsConnected = false;

            try
            {
                await WaitForTaskAsync(_packetReceiverTask).ConfigureAwait(false);
                await WaitForTaskAsync(_keepAliveMessageSenderTask).ConfigureAwait(false);

                if (_adapter != null)
                {
                    await _adapter.DisconnectAsync(_options.CommunicationTimeout, CancellationToken.None).ConfigureAwait(false);
                }

                _logger.Verbose("Disconnected from adapter.");
            }
            catch (Exception adapterException)
            {
                _logger.Warning(adapterException, "Error while disconnecting from adapter.");
            }
            finally
            {
                _adapter?.Dispose();
                _adapter = null;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                _cleanDisconnectInitiated = false;

                var disconnectException = _disconnectException;
                _disconnectException = null;

                _logger.Info("Disconnected.");
                Disconnected?.Invoke(this, new MqttClientDisconnectedEventArgs(clientWasConnected, disconnectException));
            }
        }

        private void InitiateDisconnect(Exception exception)
        {
            lock (_disconnectLock)
            {
                try
                {
                    if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
                    {
                        return;
                    }

                    _disconnectException = exception;
                    _cancellationTokenSource.Cancel(false);
                }
                catch (Exception adapterException)
                {
                    _logger.Warning(adapterException, "Error while initiating disconnect.");
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

            var packetAwaiter = _packetDispatcher.AddPacketAwaiter<TResponsePacket>(identifier);
            try
            {
                await _adapter.SendPacketAsync(requestPacket, cancellationToken).ConfigureAwait(false);
                var respone = await Internal.TaskExtensions.TimeoutAfterAsync(ct => packetAwaiter.Task, _options.CommunicationTimeout, cancellationToken).ConfigureAwait(false);

                return (TResponsePacket)respone;
            }
            catch (MqttCommunicationTimedOutException)
            {
                _logger.Warning(null, "Timeout while waiting for packet of type '{0}'.", typeof(TResponsePacket).Namespace);
                throw;
            }
            finally
            {
                _packetDispatcher.RemovePacketAwaiter<TResponsePacket>(identifier);
            }
        }

        private async Task DisconnectCleanerAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception) { }

            await DisconnectInternalAsync().ConfigureAwait(false);
        }

        private async Task SendKeepAliveMessagesAsync(CancellationToken cancellationToken)
        {
            _logger.Verbose("Start sending keep alive packets.");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var keepAliveSendInterval = TimeSpan.FromSeconds(_options.KeepAlivePeriod.TotalSeconds * 0.75);
                    if (_options.KeepAliveSendInterval.HasValue)
                    {
                        keepAliveSendInterval = _options.KeepAliveSendInterval.Value;
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

                InitiateDisconnect(exception);
            }
            finally
            {
                _logger.Verbose("Stopped sending keep alive packets.");
            }
        }

        private async Task ReceivePacketsAsync(CancellationToken cancellationToken)
        {
            _logger.Verbose("Start receiving packets.");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var packet = await _adapter.ReceivePacketAsync(TimeSpan.Zero, cancellationToken).ConfigureAwait(false);
                    if (packet != null)
                    {
                        await ProcessReceivedPacketAsync(packet, cancellationToken).ConfigureAwait(false);
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
                    _logger.Warning(exception, "MQTT communication exception while receiving packets.");
                }
                else
                {
                    _logger.Error(exception, "Unhandled exception while receiving packets.");
                }

                _packetDispatcher.Dispatch(exception);
                InitiateDisconnect(exception);
            }
            finally
            {
                _logger.Verbose("Stopped receiving packets.");
            }
        }

        private Task ProcessReceivedPacketAsync(MqttBasePacket packet, CancellationToken cancellationToken)
        {
            if (packet is MqttPublishPacket publishPacket)
            {
                return ProcessReceivedPublishPacketAsync(publishPacket, cancellationToken);
            }

            if (packet is MqttPingReqPacket)
            {
                return SendAsync(new MqttPingRespPacket(), cancellationToken);
            }

            if (packet is MqttDisconnectPacket)
            {
                return DisconnectAsync();
            }

            if (packet is MqttPubRelPacket pubRelPacket)
            {
                return ProcessReceivedPubRelPacket(pubRelPacket, cancellationToken);
            }

            _packetDispatcher.Dispatch(packet);
            return Task.FromResult(0);
        }

        private Task ProcessReceivedPublishPacketAsync(MqttPublishPacket publishPacket, CancellationToken cancellationToken)
        {
            if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtMostOnce)
            {
                FireApplicationMessageReceivedEvent(publishPacket);
                return Task.FromResult(0);
            }

            if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtLeastOnce)
            {
                FireApplicationMessageReceivedEvent(publishPacket);
                return SendAsync(new MqttPubAckPacket { PacketIdentifier = publishPacket.PacketIdentifier }, cancellationToken);
            }

            if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.ExactlyOnce)
            {
                // QoS 2 is implement as method "B" (4.3.3 QoS 2: Exactly once delivery)
                FireApplicationMessageReceivedEvent(publishPacket);
                return SendAsync(new MqttPubRecPacket { PacketIdentifier = publishPacket.PacketIdentifier }, cancellationToken);
            }

            throw new MqttCommunicationException("Received a not supported QoS level.");
        }

        private Task ProcessReceivedPubRelPacket(MqttPubRelPacket pubRelPacket, CancellationToken cancellationToken)
        {
            var response = new MqttPubCompPacket
            {
                PacketIdentifier = pubRelPacket.PacketIdentifier
            };

            return SendAsync(response, cancellationToken);
        }

        private async Task PublishExactlyOnce(MqttPublishPacket publishPacket, CancellationToken cancellationToken)
        {
            publishPacket.PacketIdentifier = _packetIdentifierProvider.GetNewPacketIdentifier();

            var pubRecPacket = await SendAndReceiveAsync<MqttPubRecPacket>(publishPacket, cancellationToken).ConfigureAwait(false);
            var pubRelPacket = new MqttPubRelPacket
            {
                PacketIdentifier = pubRecPacket.PacketIdentifier
            };

            await SendAndReceiveAsync<MqttPubCompPacket>(pubRelPacket, cancellationToken).ConfigureAwait(false);
        }

        private void StartDisconnectCleaner(CancellationToken cancellationToken)
        {
            _disconnectCleanerTask = Task.Run(
                () => DisconnectCleanerAsync(cancellationToken),
                cancellationToken);
        }

        private void StartReceivingPackets(CancellationToken cancellationToken)
        {
            _packetReceiverTask = Task.Run(
                () => ReceivePacketsAsync(cancellationToken),
                cancellationToken);
        }

        private void StartSendingKeepAliveMessages(CancellationToken cancellationToken)
        {
            _keepAliveMessageSenderTask = Task.Run(
                () => SendKeepAliveMessagesAsync(cancellationToken),
                cancellationToken);
        }

        private void FireApplicationMessageReceivedEvent(MqttPublishPacket publishPacket)
        {
            try
            {
                var applicationMessage = publishPacket.ToApplicationMessage();
                ApplicationMessageReceived?.Invoke(this, new MqttApplicationMessageReceivedEventArgs(_options.ClientId, applicationMessage));
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Unhandled exception while handling application message.");
            }
        }

        private static async Task WaitForTaskAsync(Task task)
        {
            if (task == null)
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
    }
}