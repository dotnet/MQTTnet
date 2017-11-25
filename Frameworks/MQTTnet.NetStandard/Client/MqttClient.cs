using System;
using System.Collections.Generic;
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
        private readonly HashSet<ushort> _unacknowledgedPublishPackets = new HashSet<ushort>();

        private readonly IMqttClientAdapterFactory _adapterFactory;
        private readonly MqttPacketDispatcher _packetDispatcher;
        private readonly IMqttNetLogger _logger;

        private IMqttClientOptions _options;
        private bool _isReceivingPackets;
        private int _latestPacketIdentifier;
        private CancellationTokenSource _cancellationTokenSource;
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
                _latestPacketIdentifier = 0;
                _packetDispatcher.Reset();

                _adapter = _adapterFactory.CreateClientAdapter(options.ChannelOptions, _logger);
                
                _logger.Trace<MqttClient>("Trying to connect with server.");
                await _adapter.ConnectAsync(_options.CommunicationTimeout).ConfigureAwait(false);
                _logger.Trace<MqttClient>("Connection with server established.");

                await StartReceivingPacketsAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
                var connectResponse = await AuthenticateAsync(options.WillMessage).ConfigureAwait(false);

                _logger.Trace<MqttClient>("MQTT connection with server established.");

                if (_options.KeepAlivePeriod != TimeSpan.Zero)
                {
                    StartSendingKeepAliveMessages(_cancellationTokenSource.Token);
                }

                IsConnected = true;
                Connected?.Invoke(this, new MqttClientConnectedEventArgs(connectResponse.IsSessionPresent));
                return new MqttClientConnectResult(connectResponse.IsSessionPresent);
            }
            catch (Exception exception)
            {
                _logger.Error<MqttClient>(exception, "Error while connecting with server.");
                await DisconnectInternalAsync().ConfigureAwait(false);
                throw;
            }
        }

        public async Task DisconnectAsync()
        {
            if (!IsConnected)
            {
                return;
            }

            try
            {
                await SendAsync(new MqttDisconnectPacket()).ConfigureAwait(false);
            }
            finally
            {
                await DisconnectInternalAsync().ConfigureAwait(false);
            }
        }

        public async Task<IList<MqttSubscribeResult>> SubscribeAsync(IEnumerable<TopicFilter> topicFilters)
        {
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            ThrowIfNotConnected();

            var subscribePacket = new MqttSubscribePacket
            {
                PacketIdentifier = GetNewPacketIdentifier(),
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
                PacketIdentifier = GetNewPacketIdentifier(),
                TopicFilters = topicFilters.ToList()
            };

            await SendAndReceiveAsync<MqttUnsubAckPacket>(unsubscribePacket);
        }

        public async Task PublishAsync(IEnumerable<MqttApplicationMessage> applicationMessages)
        {
            ThrowIfNotConnected();

            var publishPackets = applicationMessages.Select(m => m.ToPublishPacket());

            foreach (var qosGroup in publishPackets.GroupBy(p => p.QualityOfServiceLevel))
            {
                var qosPackets = qosGroup.ToArray();
                switch (qosGroup.Key)
                {
                    case MqttQualityOfServiceLevel.AtMostOnce:
                        {
                            // No packet identifier is used for QoS 0 [3.3.2.2 Packet Identifier]
                            await _adapter.SendPacketsAsync(_options.CommunicationTimeout, _cancellationTokenSource.Token, qosPackets).ConfigureAwait(false);
                            break;
                        }
                    case MqttQualityOfServiceLevel.AtLeastOnce:
                        {
                            foreach (var publishPacket in qosPackets)
                            {
                                publishPacket.PacketIdentifier = GetNewPacketIdentifier();
                                await SendAndReceiveAsync<MqttPubAckPacket>(publishPacket).ConfigureAwait(false);
                            }

                            break;
                        }
                    case MqttQualityOfServiceLevel.ExactlyOnce:
                        {
                            foreach (var publishPacket in qosPackets)
                            {
                                publishPacket.PacketIdentifier = GetNewPacketIdentifier();
                                var pubRecPacket = await SendAndReceiveAsync<MqttPubRecPacket>(publishPacket).ConfigureAwait(false);
                                await SendAndReceiveAsync<MqttPubCompPacket>(pubRecPacket.CreateResponse<MqttPubRelPacket>()).ConfigureAwait(false);
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

        private async Task DisconnectInternalAsync()
        {
            var clientWasConnected = IsConnected;
            IsConnected = false;

            var cts = _cancellationTokenSource;
            if (cts == null || cts.IsCancellationRequested)
            {
                return;
            }

            cts.Cancel(false);
            cts.Dispose();
            _cancellationTokenSource = null;

            try
            {
                await _adapter.DisconnectAsync(_options.CommunicationTimeout).ConfigureAwait(false);
                _logger.Info<MqttClient>("Disconnected from adapter.");
            }
            catch (Exception exception)
            {
                _logger.Warning<MqttClient>(exception, "Error while disconnecting from adapter.");
            }
            finally
            {
                _logger.Info<MqttClient>("Disconnected.");
                Disconnected?.Invoke(this, new MqttClientDisconnectedEventArgs(clientWasConnected));
            }
        }

        private async Task ProcessReceivedPacketAsync(MqttBasePacket packet)
        {
            try
            {
                _logger.Info<MqttClient>("Received <<< {0}", packet);

                if (packet is MqttPingReqPacket)
                {
                    await SendAsync(new MqttPingRespPacket());
                    return;
                }

                if (packet is MqttDisconnectPacket)
                {
                    await DisconnectAsync();
                    return;
                }

                if (packet is MqttPublishPacket publishPacket)
                {
                    await ProcessReceivedPublishPacket(publishPacket);
                    return;
                }

                if (packet is MqttPubRelPacket pubRelPacket)
                {
                    await ProcessReceivedPubRelPacket(pubRelPacket);
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

        private async Task ProcessReceivedPublishPacket(MqttPublishPacket publishPacket)
        {
            if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtMostOnce)
            {
                FireApplicationMessageReceivedEvent(publishPacket);
                return;
            }

            if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtLeastOnce)
            {
                FireApplicationMessageReceivedEvent(publishPacket);
                await SendAsync(new MqttPubAckPacket { PacketIdentifier = publishPacket.PacketIdentifier });
                return;
            }

            if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.ExactlyOnce)
            {
                // QoS 2 is implement as method "B" [4.3.3 QoS 2: Exactly once delivery]
                lock (_unacknowledgedPublishPackets)
                {
                    _unacknowledgedPublishPackets.Add(publishPacket.PacketIdentifier);
                }

                FireApplicationMessageReceivedEvent(publishPacket);
                await SendAsync(new MqttPubRecPacket { PacketIdentifier = publishPacket.PacketIdentifier });
                return;
            }

            throw new MqttCommunicationException("Received a not supported QoS level.");
        }

        private Task ProcessReceivedPubRelPacket(MqttPubRelPacket pubRelPacket)
        {
            lock (_unacknowledgedPublishPackets)
            {
                _unacknowledgedPublishPackets.Remove(pubRelPacket.PacketIdentifier);
            }

            return SendAsync(pubRelPacket.CreateResponse<MqttPubCompPacket>());
        }

        private Task SendAsync(MqttBasePacket packet)
        {
            return _adapter.SendPacketsAsync(_options.CommunicationTimeout, _cancellationTokenSource.Token, packet);
        }

        private async Task<TResponsePacket> SendAndReceiveAsync<TResponsePacket>(MqttBasePacket requestPacket) where TResponsePacket : MqttBasePacket
        {
            var packetAwaiter = _packetDispatcher.WaitForPacketAsync(requestPacket, typeof(TResponsePacket), _options.CommunicationTimeout);
            await _adapter.SendPacketsAsync(_options.CommunicationTimeout, _cancellationTokenSource.Token, requestPacket).ConfigureAwait(false);
            return (TResponsePacket)await packetAwaiter.ConfigureAwait(false);
        }

        private ushort GetNewPacketIdentifier()
        {
            return (ushort)Interlocked.Increment(ref _latestPacketIdentifier);
        }

        private async Task SendKeepAliveMessagesAsync(CancellationToken cancellationToken)
        {
            _logger.Info<MqttClient>("Start sending keep alive packets.");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(_options.KeepAlivePeriod, cancellationToken).ConfigureAwait(false);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    await SendAndReceiveAsync<MqttPingRespPacket>(new MqttPingReqPacket()).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await DisconnectInternalAsync().ConfigureAwait(false);
            }
            catch (MqttCommunicationException exception)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                _logger.Warning<MqttClient>(exception, "MQTT communication exception while sending/receiving keep alive packets.");
                await DisconnectInternalAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.Warning<MqttClient>(exception, "Unhandled exception while sending/receiving keep alive packets.");
                await DisconnectInternalAsync().ConfigureAwait(false);
            }
            finally
            {
                _logger.Info<MqttClient>("Stopped sending keep alive packets.");
            }
        }

        private async Task ReceivePacketsAsync(CancellationToken cancellationToken)
        {
            _logger.Info<MqttClient>("Start receiving packets.");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    _isReceivingPackets = true;

                    var packet = await _adapter.ReceivePacketAsync(TimeSpan.Zero, cancellationToken).ConfigureAwait(false);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    StartProcessReceivedPacket(packet, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await DisconnectInternalAsync().ConfigureAwait(false);
            }
            catch (MqttCommunicationException exception)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                _logger.Warning<MqttClient>(exception, "MQTT communication exception while receiving packets.");
                await DisconnectInternalAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.Error<MqttClient>(exception, "Unhandled exception while receiving packets.");
                await DisconnectInternalAsync().ConfigureAwait(false);
            }
            finally
            {
                _logger.Info<MqttClient>("Stopped receiving packets.");
            }
        }

        private void StartProcessReceivedPacket(MqttBasePacket packet, CancellationToken cancellationToken)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(
                () => ProcessReceivedPacketAsync(packet),
                cancellationToken).ConfigureAwait(false);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        private async Task StartReceivingPacketsAsync(CancellationToken cancellationToken)
        {
            _isReceivingPackets = false;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(
                async () => await ReceivePacketsAsync(cancellationToken),
                cancellationToken).ConfigureAwait(false);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            while (!_isReceivingPackets && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken).ConfigureAwait(false);
            }
        }

        private void StartSendingKeepAliveMessages(CancellationToken cancellationToken)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(
                async () => await SendKeepAliveMessagesAsync(cancellationToken), 
                cancellationToken).ConfigureAwait(false);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }
    }
}