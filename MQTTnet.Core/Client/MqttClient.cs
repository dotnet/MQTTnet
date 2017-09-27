using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Exceptions;
using MQTTnet.Core.Internal;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;

namespace MQTTnet.Core.Client
{
    public class MqttClient : IMqttClient
    {
        private readonly HashSet<ushort> _unacknowledgedPublishPackets = new HashSet<ushort>();
        private readonly MqttPacketDispatcher _packetDispatcher = new MqttPacketDispatcher();
        private readonly MqttClientOptions _options;
        private readonly IMqttCommunicationAdapter _adapter;

        private int _latestPacketIdentifier;
        private CancellationTokenSource _cancellationTokenSource;

        public MqttClient(MqttClientOptions options, IMqttCommunicationAdapter adapter)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));

            _adapter.PacketSerializer.ProtocolVersion = options.ProtocolVersion;
        }

        public event EventHandler Connected;
        public event EventHandler Disconnected;
        public event EventHandler<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;

        public bool IsConnected => _cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested;

        public async Task ConnectAsync(MqttApplicationMessage willApplicationMessage = null)
        {
            ThrowIfConnected("It is not allowed to connect with a server after the connection is established.");

            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _latestPacketIdentifier = 0;
                _packetDispatcher.Reset();

                MqttTrace.Verbose(nameof(MqttClient), "Trying to connect with server.");
                await _adapter.ConnectAsync(_options.DefaultCommunicationTimeout, _options).ConfigureAwait(false);
                MqttTrace.Verbose(nameof(MqttClient), "Connection with server established.");

                StartReceivePackets(_cancellationTokenSource.Token);

                var connectPacket = new MqttConnectPacket
                {
                    ClientId = _options.ClientId,
                    Username = _options.UserName,
                    Password = _options.Password,
                    CleanSession = _options.CleanSession,
                    KeepAlivePeriod = (ushort)_options.KeepAlivePeriod.TotalSeconds,
                    WillMessage = willApplicationMessage
                };

                var response = await SendAndReceiveAsync<MqttConnAckPacket>(connectPacket).ConfigureAwait(false);
                
                if (response.ConnectReturnCode != MqttConnectReturnCode.ConnectionAccepted)
                {
                    throw new MqttConnectingFailedException(response.ConnectReturnCode);
                }

                MqttTrace.Verbose(nameof(MqttClient), "MQTT connection with server established.");

                if (_options.KeepAlivePeriod != TimeSpan.Zero)
                {
                    StartSendKeepAliveMessages(_cancellationTokenSource.Token);
                }

                Connected?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception)
            {
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
                            await _adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, _cancellationTokenSource.Token, qosPackets);
                            break;
                        }
                    case MqttQualityOfServiceLevel.AtLeastOnce:
                        {
                            foreach (var publishPacket in qosPackets)
                            {
                                publishPacket.PacketIdentifier = GetNewPacketIdentifier();
                                await SendAndReceiveAsync<MqttPubAckPacket>(publishPacket);
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
                await _adapter.DisconnectAsync(_options.DefaultCommunicationTimeout).ConfigureAwait(false);
                MqttTrace.Information(nameof(MqttClient), "Disconnected from adapter.");
            }
            catch (Exception exception)
            {
                MqttTrace.Warning(nameof(MqttClient), exception, "Error while disconnecting from adapter.");
            }
            finally
            {
                Disconnected?.Invoke(this, EventArgs.Empty);
            }
        }

        private async Task ProcessReceivedPacketAsync(MqttBasePacket packet)
        {
            try
            {
                MqttTrace.Information(nameof(MqttClient), "Received <<< {0}", packet);

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
                MqttTrace.Error(nameof(MqttClient), exception, "Unhandled exception while processing received packet.");
            }
        }

        private void FireApplicationMessageReceivedEvent(MqttPublishPacket publishPacket)
        {
            try
            {
                var applicationMessage = publishPacket.ToApplicationMessage();
                ApplicationMessageReceived?.Invoke(this, new MqttApplicationMessageReceivedEventArgs(applicationMessage));
            }
            catch (Exception exception)
            {
                MqttTrace.Error(nameof(MqttClient), exception, "Unhandled exception while handling application message.");
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
            return _adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, _cancellationTokenSource.Token, packet);
        }

        private async Task<TResponsePacket> SendAndReceiveAsync<TResponsePacket>(MqttBasePacket requestPacket) where TResponsePacket : MqttBasePacket
        {
            var wait = _packetDispatcher.WaitForPacketAsync(requestPacket, typeof(TResponsePacket), _options.DefaultCommunicationTimeout);
            await _adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, _cancellationTokenSource.Token, requestPacket).ConfigureAwait(false);
            return (TResponsePacket)await wait.ConfigureAwait(false);
        }

        private ushort GetNewPacketIdentifier()
        {
            return (ushort)Interlocked.Increment(ref _latestPacketIdentifier);
        }

        private async Task SendKeepAliveMessagesAsync(CancellationToken cancellationToken)
        {
            MqttTrace.Information(nameof(MqttClient), "Start sending keep alive packets.");

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
            catch (TaskCanceledException)
            {
            }
            catch (MqttCommunicationException exception)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                MqttTrace.Warning(nameof(MqttClient), exception, "MQTT communication exception while sending/receiving keep alive packets.");
                await DisconnectInternalAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                MqttTrace.Warning(nameof(MqttClient), exception, "Unhandled exception while sending/receiving keep alive packets.");
                await DisconnectInternalAsync().ConfigureAwait(false);
            }
            finally
            {
                MqttTrace.Information(nameof(MqttClient), "Stopped sending keep alive packets.");
            }
        }

        private async Task ReceivePackets(CancellationToken cancellationToken)
        {
            MqttTrace.Information(nameof(MqttClient), "Start receiving packets.");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var packet = await _adapter.ReceivePacketAsync(TimeSpan.Zero, cancellationToken).ConfigureAwait(false);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    StartProcessReceivedPacket(packet, cancellationToken);
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch (MqttCommunicationException exception)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                MqttTrace.Warning(nameof(MqttClient), exception, "MQTT communication exception while receiving packets.");
                await DisconnectInternalAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                MqttTrace.Error(nameof(MqttClient), exception, "Unhandled exception while receiving packets.");
                await DisconnectInternalAsync().ConfigureAwait(false);
            }
            finally
            {
                MqttTrace.Information(nameof(MqttClient), "Stopped receiving packets.");
            }
        }

        private void StartProcessReceivedPacket(MqttBasePacket packet, CancellationToken cancellationToken)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(() => ProcessReceivedPacketAsync(packet), cancellationToken).ConfigureAwait(false);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        private void StartReceivePackets(CancellationToken cancellationToken)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(() => ReceivePackets(cancellationToken), cancellationToken).ConfigureAwait(false); ;
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        private void StartSendKeepAliveMessages(CancellationToken cancellationToken)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(() => SendKeepAliveMessagesAsync(cancellationToken), cancellationToken).ConfigureAwait(false);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }
    }
}