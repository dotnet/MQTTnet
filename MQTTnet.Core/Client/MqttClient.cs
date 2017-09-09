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

        private bool _disconnectedEventSuspended;
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

        public bool IsConnected { get; private set; }

        public async Task ConnectAsync(MqttApplicationMessage willApplicationMessage = null)
        {
            MqttTrace.Verbose(nameof(MqttClient), "Trying to connect.");

            if (IsConnected)
            {
                throw new MqttProtocolViolationException("It is not allowed to connect with a server after the connection is established.");
            }

            try
            {
                _disconnectedEventSuspended = false;

                await _adapter.ConnectAsync(_options, _options.DefaultCommunicationTimeout).ConfigureAwait(false);

                MqttTrace.Verbose(nameof(MqttClient), "Connection with server established.");

                var connectPacket = new MqttConnectPacket
                {
                    ClientId = _options.ClientId,
                    Username = _options.UserName,
                    Password = _options.Password,
                    CleanSession = _options.CleanSession,
                    KeepAlivePeriod = (ushort)_options.KeepAlivePeriod.TotalSeconds,
                    WillMessage = willApplicationMessage
                };

                _cancellationTokenSource = new CancellationTokenSource();
                _latestPacketIdentifier = 0;
                _packetDispatcher.Reset();

                StartReceivePackets();

                var response = await SendAndReceiveAsync<MqttConnAckPacket>(connectPacket).ConfigureAwait(false);
                if (response.ConnectReturnCode != MqttConnectReturnCode.ConnectionAccepted)
                {
                    await DisconnectInternalAsync().ConfigureAwait(false);
                    throw new MqttConnectingFailedException(response.ConnectReturnCode);
                }

                if (_options.KeepAlivePeriod != TimeSpan.Zero)
                {
                    StartSendKeepAliveMessages();
                }

                MqttTrace.Verbose(nameof(MqttClient), "MQTT connection with server established.");

                IsConnected = true;
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
            try
            {
                await SendAsync(new MqttDisconnectPacket()).ConfigureAwait(false);
            }
            finally
            {
                await DisconnectInternalAsync().ConfigureAwait(false);
            }
        }

        public Task<IList<MqttSubscribeResult>> SubscribeAsync(params TopicFilter[] topicFilters)
        {
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            return SubscribeAsync(topicFilters.ToList());
        }

        public async Task<IList<MqttSubscribeResult>> SubscribeAsync(IList<TopicFilter> topicFilters)
        {
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));
            if (!topicFilters.Any()) throw new MqttProtocolViolationException("At least one topic filter must be set [MQTT-3.8.3-3].");

            ThrowIfNotConnected();

            var subscribePacket = new MqttSubscribePacket
            {
                PacketIdentifier = GetNewPacketIdentifier(),
                TopicFilters = topicFilters
            };

            var response = await SendAndReceiveAsync<MqttSubAckPacket>(subscribePacket).ConfigureAwait(false);

            if (response.SubscribeReturnCodes.Count != topicFilters.Count)
            {
                throw new MqttProtocolViolationException("The return codes are not matching the topic filters [MQTT-3.9.3-1].");
            }

            return topicFilters.Select((t, i) => new MqttSubscribeResult(t, response.SubscribeReturnCodes[i])).ToList();
        }

        public Task Unsubscribe(params string[] topicFilters)
        {
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));

            return Unsubscribe(topicFilters.ToList());
        }

        public Task Unsubscribe(IList<string> topicFilters)
        {
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));
            if (!topicFilters.Any()) throw new MqttProtocolViolationException("At least one topic filter must be set [MQTT-3.10.3-2].");
            ThrowIfNotConnected();

            var unsubscribePacket = new MqttUnsubscribePacket
            {
                PacketIdentifier = GetNewPacketIdentifier(),
                TopicFilters = topicFilters
            };

            return SendAndReceiveAsync<MqttUnsubAckPacket>(unsubscribePacket);
        }

        public Task PublishAsync(MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));
            ThrowIfNotConnected();

            var publishPacket = applicationMessage.ToPublishPacket();

            if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtMostOnce)
            {
                // No packet identifier is used for QoS 0 [3.3.2.2 Packet Identifier]
                return SendAsync(publishPacket);
            }

            if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtLeastOnce)
            {
                publishPacket.PacketIdentifier = GetNewPacketIdentifier();
                return SendAndReceiveAsync<MqttPubAckPacket>(publishPacket);
            }

            if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.ExactlyOnce)
            {
                publishPacket.PacketIdentifier = GetNewPacketIdentifier();
                return PublishExactlyOncePacketAsync(publishPacket);
            }

            throw new InvalidOperationException();
        }

        private async Task PublishExactlyOncePacketAsync(MqttBasePacket publishPacket)
        {
            var pubRecPacket = await SendAndReceiveAsync<MqttPubRecPacket>(publishPacket).ConfigureAwait(false);
            await SendAndReceiveAsync<MqttPubCompPacket>(pubRecPacket.CreateResponse<MqttPubRelPacket>()).ConfigureAwait(false);
        }

        private void ThrowIfNotConnected()
        {
            if (!IsConnected) throw new MqttCommunicationException("The client is not connected.");
        }

        private Task DisconnectInternalAsync()
        {
            try
            {
                return _adapter.DisconnectAsync();
            }
            catch (Exception exception)
            {
                MqttTrace.Warning(nameof(MqttClient), exception, "Error while disconnecting.");
                return Task.FromResult(0);
            }
            finally
            {
                _cancellationTokenSource?.Cancel(false);
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                IsConnected = false;

                if (!_disconnectedEventSuspended)
                {
                    _disconnectedEventSuspended = true;
                    Disconnected?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private Task ProcessReceivedPacketAsync(MqttBasePacket mqttPacket)
        {
            try
            {
                if (mqttPacket is MqttPingReqPacket)
                {
                    return SendAsync(new MqttPingRespPacket());
                }

                if (mqttPacket is MqttDisconnectPacket)
                {
                    return DisconnectAsync();
                }

                if (mqttPacket is MqttPublishPacket publishPacket)
                {
                    return ProcessReceivedPublishPacket(publishPacket);
                }

                if (mqttPacket is MqttPubRelPacket pubRelPacket)
                {
                    return ProcessReceivedPubRelPacket(pubRelPacket);
                }

                _packetDispatcher.Dispatch(mqttPacket);
            }
            catch (Exception exception)
            {
                MqttTrace.Error(nameof(MqttClient), exception, "Error while processing received packet.");
            }

            return Task.FromResult(0);
        }

        private void FireApplicationMessageReceivedEvent(MqttPublishPacket publishPacket)
        {
            var applicationMessage = publishPacket.ToApplicationMessage();

            try
            {
                ApplicationMessageReceived?.Invoke(this, new MqttApplicationMessageReceivedEventArgs(applicationMessage));
            }
            catch (Exception exception)
            {
                MqttTrace.Error(nameof(MqttClient), exception, "Unhandled exception while handling application message.");
            }
        }

        private Task ProcessReceivedPublishPacket(MqttPublishPacket publishPacket)
        {
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
                lock (_unacknowledgedPublishPackets)
                {
                    _unacknowledgedPublishPackets.Add(publishPacket.PacketIdentifier);
                }

                FireApplicationMessageReceivedEvent(publishPacket);
                return SendAsync(new MqttPubRecPacket { PacketIdentifier = publishPacket.PacketIdentifier });
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
            return _adapter.SendPacketAsync(packet, _options.DefaultCommunicationTimeout);
        }

        private async Task<TResponsePacket> SendAndReceiveAsync<TResponsePacket>(MqttBasePacket requestPacket) where TResponsePacket : MqttBasePacket
        {
            bool ResponsePacketSelector(MqttBasePacket p)
            {
                if (!(p is TResponsePacket p1))
                {
                    return false;
                }

                if (!(requestPacket is IMqttPacketWithIdentifier pi1) || !(p is IMqttPacketWithIdentifier pi2))
                {
                    return true;
                }

                return pi1.PacketIdentifier == pi2.PacketIdentifier;
            }

            await _adapter.SendPacketAsync(requestPacket, _options.DefaultCommunicationTimeout).ConfigureAwait(false);
            return (TResponsePacket)await _packetDispatcher.WaitForPacketAsync(ResponsePacketSelector, _options.DefaultCommunicationTimeout).ConfigureAwait(false);
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
                    await SendAndReceiveAsync<MqttPingRespPacket>(new MqttPingReqPacket()).ConfigureAwait(false);
                }
            }
            catch (MqttCommunicationException exception)
            {
                MqttTrace.Warning(nameof(MqttClient), exception, "MQTT communication error while receiving packets.");
                await DisconnectInternalAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                MqttTrace.Warning(nameof(MqttClient), exception, "Error while sending/receiving keep alive packets.");
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
                    var packet = await _adapter.ReceivePacketAsync(TimeSpan.Zero).ConfigureAwait(false);
                    MqttTrace.Information(nameof(MqttClient), "Received <<< {0}", packet);

                    StartProcessReceivedPacket(packet, cancellationToken);
                }
            }
            catch (MqttCommunicationException exception)
            {
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
            Task.Run(() => ProcessReceivedPacketAsync(packet), cancellationToken);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        private void StartReceivePackets()
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(() => ReceivePackets(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        private void StartSendKeepAliveMessages()
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(() => SendKeepAliveMessagesAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }
    }
}