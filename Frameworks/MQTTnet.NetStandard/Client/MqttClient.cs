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
        private readonly SemaphoreSlim _disconnectLock = new SemaphoreSlim(1, 1);
        private readonly IMqttClientAdapterFactory _adapterFactory;
        private readonly MqttPacketDispatcher _packetDispatcher;
        private readonly IMqttNetLogger _logger;

        private IMqttClientOptions _options;
        private bool _isReceivingPackets;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _packetReceiverTask;
        private Task _keepAliveMessageSenderTask;
        private IMqttChannelAdapter _adapter;

        public MqttClient(IMqttClientAdapterFactory channelFactory, IMqttNetLogger logger)
        {
            _adapterFactory = channelFactory ?? throw new ArgumentNullException(nameof(channelFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _packetDispatcher = new MqttPacketDispatcher(logger);
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
                _options = options;
                _cancellationTokenSource = new CancellationTokenSource();
                _packetIdentifierProvider.Reset();
                _packetDispatcher.Reset();

                _adapter = _adapterFactory.CreateClientAdapter(options, _logger);

                _logger.Verbose<MqttClient>("Trying to connect with server.");
                await _adapter.ConnectAsync(_options.CommunicationTimeout, _cancellationTokenSource.Token).ConfigureAwait(false);
                _logger.Verbose<MqttClient>("Connection with server established.");

                await StartReceivingPacketsAsync().ConfigureAwait(false);

                var connectResponse = await AuthenticateAsync(options.WillMessage).ConfigureAwait(false);
                _logger.Verbose<MqttClient>("MQTT connection with server established.");

                _sendTracker.Restart();

                if (_options.KeepAlivePeriod != TimeSpan.Zero)
                {
                    StartSendingKeepAliveMessages();
                }

                IsConnected = true;
                Connected?.Invoke(this, new MqttClientConnectedEventArgs(connectResponse.IsSessionPresent));

                _logger.Info<MqttClient>("Connected.");
                return new MqttClientConnectResult(connectResponse.IsSessionPresent);
            }
            catch (Exception exception)
            {
                _logger.Error<MqttClient>(exception, "Error while connecting with server.");
                await DisconnectInternalAsync(null, exception).ConfigureAwait(false);

                throw;
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                if (IsConnected && !_cancellationTokenSource.IsCancellationRequested)
                {
                    await SendAsync(new MqttDisconnectPacket()).ConfigureAwait(false);
                }
            }
            finally
            {
                await DisconnectInternalAsync(null, null).ConfigureAwait(false);
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

            var response = await SendAndReceiveAsync<MqttSubAckPacket>(subscribePacket).ConfigureAwait(false);

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

            await SendAndReceiveAsync<MqttUnsubAckPacket>(unsubscribePacket).ConfigureAwait(false);
        }

        public async Task PublishAsync(IEnumerable<MqttApplicationMessage> applicationMessages)
        {
            ThrowIfNotConnected();

            var publishPackets = applicationMessages.Select(m => m.ToPublishPacket());
            var packetGroups = publishPackets.GroupBy(p => p.QualityOfServiceLevel).OrderBy(g => g.Key);

            foreach (var qosGroup in packetGroups)
            {
                switch (qosGroup.Key)
                {
                    case MqttQualityOfServiceLevel.AtMostOnce:
                        {
                            // No packet identifier is used for QoS 0 [3.3.2.2 Packet Identifier]
                            await SendAsync(qosGroup.Cast<MqttBasePacket>().ToArray()).ConfigureAwait(false);
                            break;
                        }
                    case MqttQualityOfServiceLevel.AtLeastOnce:
                        {
                            foreach (var publishPacket in qosGroup)
                            {
                                publishPacket.PacketIdentifier = _packetIdentifierProvider.GetNewPacketIdentifier();
                                await SendAndReceiveAsync<MqttPubAckPacket>(publishPacket).ConfigureAwait(false);
                            }

                            break;
                        }
                    case MqttQualityOfServiceLevel.ExactlyOnce:
                        {
                            foreach (var publishPacket in qosGroup)
                            {
                                publishPacket.PacketIdentifier = _packetIdentifierProvider.GetNewPacketIdentifier();

                                var pubRecPacket = await SendAndReceiveAsync<MqttPubRecPacket>(publishPacket).ConfigureAwait(false);
                                var pubRelPacket = new MqttPubRelPacket
                                {
                                    PacketIdentifier = pubRecPacket.PacketIdentifier
                                };

                                await SendAndReceiveAsync<MqttPubCompPacket>(pubRelPacket).ConfigureAwait(false);
                            }

                            break;
                        }
                    default:
                        {
                            throw new InvalidOperationException();
                        }
                }
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            _adapter?.Dispose();
        }

        private async Task<MqttConnAckPacket> AuthenticateAsync(MqttApplicationMessage willApplicationMessage)
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

            var response = await SendAndReceiveAsync<MqttConnAckPacket>(connectPacket).ConfigureAwait(false);
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

        private async Task DisconnectInternalAsync(Task sender, Exception exception)
        {
            await _disconnectLock.WaitAsync();
            try
            {
                if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }

                _cancellationTokenSource.Cancel(false);
            }
            catch (Exception adapterException)
            {
                _logger.Warning<MqttClient>(adapterException, "Error while disconnecting from adapter.");
            }
            finally
            {
                _disconnectLock.Release();
            }

            var clientWasConnected = IsConnected;
            IsConnected = false;

            try
            {
                if (_packetReceiverTask != null && _packetReceiverTask != sender)
                {
                    _packetReceiverTask.Wait();
                }

                if (_keepAliveMessageSenderTask != null && _keepAliveMessageSenderTask != sender)
                {
                    _keepAliveMessageSenderTask.Wait();
                }

                if (_adapter != null)
                {
                    await _adapter.DisconnectAsync(_options.CommunicationTimeout).ConfigureAwait(false);
                }
                
                _logger.Verbose<MqttClient>("Disconnected from adapter.");
            }
            catch (Exception adapterException)
            {
                _logger.Warning<MqttClient>(adapterException, "Error while disconnecting from adapter.");
            }
            finally
            {
                _adapter?.Dispose();
                _adapter = null;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                _logger.Info<MqttClient>("Disconnected.");
                Disconnected?.Invoke(this, new MqttClientDisconnectedEventArgs(clientWasConnected, exception));
            }
        }

        private async Task ProcessReceivedPacketAsync(MqttBasePacket packet)
        {
            try
            {
                if (packet is MqttPublishPacket publishPacket)
                {
                    await ProcessReceivedPublishPacketAsync(publishPacket).ConfigureAwait(false);
                    return;
                }

                if (packet is MqttPingReqPacket)
                {
                    await SendAsync(new MqttPingRespPacket()).ConfigureAwait(false);
                    return;
                }

                if (packet is MqttDisconnectPacket)
                {
                    await DisconnectAsync().ConfigureAwait(false);
                    return;
                }

                if (packet is MqttPubRelPacket pubRelPacket)
                {
                    await ProcessReceivedPubRelPacket(pubRelPacket).ConfigureAwait(false);
                    return;
                }

                _packetDispatcher.Dispatch(packet);
            }
            catch (Exception exception)
            {
                _logger.Error<MqttClient>(exception, "Unhandled exception while processing received packet.");
            }
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
                _logger.Error<MqttClient>(exception, "Unhandled exception while handling application message.");
            }
        }

        private Task ProcessReceivedPublishPacketAsync(MqttPublishPacket publishPacket)
        {
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                return Task.FromResult(0);
            }

            if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtMostOnce)
            {
                FireApplicationMessageReceivedEvent(publishPacket);
                return Task.FromResult(0);
            }

            if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtLeastOnce)
            {
                FireApplicationMessageReceivedEvent(publishPacket);
                return SendAsync(new MqttPubAckPacket { PacketIdentifier = publishPacket.PacketIdentifier });
            }

            if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.ExactlyOnce)
            {
                // QoS 2 is implement as method "B" [4.3.3 QoS 2: Exactly once delivery]
                FireApplicationMessageReceivedEvent(publishPacket);
                return SendAsync(new MqttPubRecPacket { PacketIdentifier = publishPacket.PacketIdentifier });
            }

            throw new MqttCommunicationException("Received a not supported QoS level.");
        }

        private Task ProcessReceivedPubRelPacket(MqttPubRelPacket pubRelPacket)
        {
            var response = new MqttPubCompPacket
            {
                PacketIdentifier = pubRelPacket.PacketIdentifier
            };

            return SendAsync(response);
        }

        private Task SendAsync(params MqttBasePacket[] packets)
        {
            _sendTracker.Restart();
            return _adapter.SendPacketsAsync(_options.CommunicationTimeout, _cancellationTokenSource.Token, packets);
        }

        private async Task<TResponsePacket> SendAndReceiveAsync<TResponsePacket>(MqttBasePacket requestPacket) where TResponsePacket : MqttBasePacket
        {
            ushort? identifier = null;
            if (requestPacket is IMqttPacketWithIdentifier requestPacketWithIdentifier)
            {
                identifier = requestPacketWithIdentifier.PacketIdentifier;
            }

            var packetAwaiter = _packetDispatcher.WaitForPacketAsync(typeof(TResponsePacket), identifier, _options.CommunicationTimeout);
            await SendAsync(requestPacket).ConfigureAwait(false);

            return (TResponsePacket)await packetAwaiter.ConfigureAwait(false);
        }

        private async Task SendKeepAliveMessagesAsync()
        {
            _logger.Verbose<MqttClient>("Start sending keep alive packets.");

            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var keepAliveSendInterval = TimeSpan.FromSeconds(_options.KeepAlivePeriod.TotalSeconds * 0.75);
                    if (_options.KeepAliveSendInterval.HasValue)
                    {
                        keepAliveSendInterval = _options.KeepAliveSendInterval.Value;
                    }

                    if (_sendTracker.Elapsed > keepAliveSendInterval)
                    {
                        await SendAndReceiveAsync<MqttPingRespPacket>(new MqttPingReqPacket()).ConfigureAwait(false);
                    }

                    await Task.Delay(keepAliveSendInterval, _cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                if (exception is OperationCanceledException)
                {
                }
                else if (exception is MqttCommunicationException)
                {
                    _logger.Warning<MqttClient>(exception, "MQTT communication exception while sending/receiving keep alive packets.");
                }
                else
                {
                    _logger.Error<MqttClient>(exception, "Unhandled exception while sending/receiving keep alive packets.");
                }
                
                await DisconnectInternalAsync(_keepAliveMessageSenderTask, exception).ConfigureAwait(false);
            }
            finally
            {
                _logger.Verbose<MqttClient>("Stopped sending keep alive packets.");
            }
        }

        private async Task ReceivePacketsAsync()
        {
            _logger.Verbose<MqttClient>("Start receiving packets.");

            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    _isReceivingPackets = true;

                    var packet = await _adapter.ReceivePacketAsync(TimeSpan.Zero, _cancellationTokenSource.Token).ConfigureAwait(false);

                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        return;
                    }

                    StartProcessReceivedPacket(packet);
                }
            }
            catch (Exception exception)
            {
                if (exception is OperationCanceledException)
                {
                }
                else if (exception is MqttCommunicationException)
                {
                    _logger.Warning<MqttClient>(exception, "MQTT communication exception while receiving packets.");
                }
                else
                {
                    _logger.Error<MqttClient>(exception, "Unhandled exception while receiving packets.");
                }

                await DisconnectInternalAsync(_packetReceiverTask, exception).ConfigureAwait(false);
            }
            finally
            {
                _logger.Verbose<MqttClient>("Stopped receiving packets.");
            }
        }

        private void StartProcessReceivedPacket(MqttBasePacket packet)
        {
            Task.Run(() => ProcessReceivedPacketAsync(packet), _cancellationTokenSource.Token);
        }

        private async Task StartReceivingPacketsAsync()
        {
            _isReceivingPackets = false;

            _packetReceiverTask = Task.Run(ReceivePacketsAsync, _cancellationTokenSource.Token);

            while (!_isReceivingPackets && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), _cancellationTokenSource.Token).ConfigureAwait(false);
            }
        }

        private void StartSendingKeepAliveMessages()
        {
            _keepAliveMessageSenderTask = Task.Run(SendKeepAliveMessagesAsync, _cancellationTokenSource.Token);
        }
    }
}