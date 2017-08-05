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

            var connectPacket = new MqttConnectPacket
            {
                ClientId = _options.ClientId,
                Username = _options.UserName,
                Password = _options.Password,
                CleanSession = _options.CleanSession,
                KeepAlivePeriod = (ushort)_options.KeepAlivePeriod.TotalSeconds,
                WillMessage = willApplicationMessage
            };

            await _adapter.ConnectAsync(_options, _options.DefaultCommunicationTimeout);
            MqttTrace.Verbose(nameof(MqttClient), "Connection with server established.");

            _cancellationTokenSource = new CancellationTokenSource();
            _latestPacketIdentifier = 0;
            _packetDispatcher.Reset();
            IsConnected = true;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(() => ReceivePackets(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            var response = await SendAndReceiveAsync<MqttConnAckPacket>(connectPacket);
            if (response.ConnectReturnCode != MqttConnectReturnCode.ConnectionAccepted)
            {
                await DisconnectAsync();
                throw new MqttConnectingFailedException(response.ConnectReturnCode);
            }

            if (_options.KeepAlivePeriod != TimeSpan.Zero)
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Run(() => SendKeepAliveMessagesAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }

            Connected?.Invoke(this, EventArgs.Empty);
        }

        public async Task DisconnectAsync()
        {
            await SendAsync(new MqttDisconnectPacket());
            await DisconnectInternalAsync();
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

            var response = await SendAndReceiveAsync<MqttSubAckPacket>(subscribePacket);

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

        public async Task Unsubscribe(IList<string> topicFilters)
        {
            if (topicFilters == null) throw new ArgumentNullException(nameof(topicFilters));
            if (!topicFilters.Any()) throw new MqttProtocolViolationException("At least one topic filter must be set [MQTT-3.10.3-2].");
            ThrowIfNotConnected();

            var unsubscribePacket = new MqttUnsubscribePacket
            {
                PacketIdentifier = GetNewPacketIdentifier(),
                TopicFilters = topicFilters
            };

            await SendAndReceiveAsync<MqttUnsubAckPacket>(unsubscribePacket);
        }

        public async Task PublishAsync(MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));
            ThrowIfNotConnected();

            var publishPacket = applicationMessage.ToPublishPacket();

            if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtMostOnce)
            {
                // No packet identifier is used for QoS 0 [3.3.2.2 Packet Identifier]
                await SendAsync(publishPacket);
            }
            else if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtLeastOnce)
            {
                publishPacket.PacketIdentifier = GetNewPacketIdentifier();
                await SendAndReceiveAsync<MqttPubAckPacket>(publishPacket);
            }
            else if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.ExactlyOnce)
            {
                publishPacket.PacketIdentifier = GetNewPacketIdentifier();
                var pubRecPacket = await SendAndReceiveAsync<MqttPubRecPacket>(publishPacket);
                await SendAndReceiveAsync<MqttPubCompPacket>(pubRecPacket.CreateResponse<MqttPubRelPacket>());
            }
        }

        private void ThrowIfNotConnected()
        {
            if (!IsConnected) throw new MqttCommunicationException("The client is not connected.");
        }

        private async Task DisconnectInternalAsync()
        {
            try
            {
                await _adapter.DisconnectAsync();
            }
            catch
            {
            }
            finally
            {
                _cancellationTokenSource?.Cancel(false);
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                IsConnected = false;
                Disconnected?.Invoke(this, EventArgs.Empty);
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

            throw new InvalidOperationException();
        }

        private async Task ProcessReceivedPubRelPacket(MqttPubRelPacket pubRelPacket)
        {
            lock (_unacknowledgedPublishPackets)
            {
                _unacknowledgedPublishPackets.Remove(pubRelPacket.PacketIdentifier);
            }
            
            await SendAsync(pubRelPacket.CreateResponse<MqttPubCompPacket>());
        }

        private Task SendAsync(MqttBasePacket packet)
        {
            return _adapter.SendPacketAsync(packet, _options.DefaultCommunicationTimeout);
        }

        private async Task<TResponsePacket> SendAndReceiveAsync<TResponsePacket>(MqttBasePacket requestPacket) where TResponsePacket : MqttBasePacket
        {
            bool ResponsePacketSelector(MqttBasePacket p)
            {
                var p1 = p as TResponsePacket;
                if (p1 == null)
                {
                    return false;
                }

                var pi1 = requestPacket as IMqttPacketWithIdentifier;
                var pi2 = p as IMqttPacketWithIdentifier;

                if (pi1 != null && pi2 != null)
                {
                    if (pi1.PacketIdentifier != pi2.PacketIdentifier)
                    {
                        return false;
                    }
                }

                return true;
            }

            await _adapter.SendPacketAsync(requestPacket, _options.DefaultCommunicationTimeout);
            return (TResponsePacket)await _packetDispatcher.WaitForPacketAsync(ResponsePacketSelector, _options.DefaultCommunicationTimeout);
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
                    await Task.Delay(_options.KeepAlivePeriod, cancellationToken);
                    await SendAndReceiveAsync<MqttPingRespPacket>(new MqttPingReqPacket());
                }
            }
            catch (MqttCommunicationException exception)
            {
                MqttTrace.Warning(nameof(MqttClient), exception, "MQTT communication error while receiving packets.");
            }
            catch (Exception exception)
            {
                MqttTrace.Warning(nameof(MqttClient), exception, "Error while sending/receiving keep alive packets.");
            }
            finally
            {
                MqttTrace.Information(nameof(MqttClient), "Stopped sending keep alive packets.");
                await DisconnectInternalAsync();
            }
        }

        private async Task ReceivePackets(CancellationToken cancellationToken)
        {
            MqttTrace.Information(nameof(MqttClient), "Start receiving packets.");
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var mqttPacket = await _adapter.ReceivePacketAsync(TimeSpan.Zero);
                    MqttTrace.Information(nameof(MqttClient), $"Received <<< {mqttPacket}");

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    Task.Run(() => ProcessReceivedPacketAsync(mqttPacket), cancellationToken);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                }
            }
            catch (MqttCommunicationException exception)
            {
                MqttTrace.Warning(nameof(MqttClient), exception, "MQTT communication error while receiving packets.");
            }
            catch (Exception exception)
            {
                MqttTrace.Error(nameof(MqttClient), exception, "Error while receiving packets.");
            }
            finally
            {
                MqttTrace.Information(nameof(MqttClient), "Stopped receiving packets.");
                await DisconnectInternalAsync();
            }
        }
    }
}