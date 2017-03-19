using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Exceptions;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;

namespace MQTTnet.Core.Client
{
    public class MqttClient
    {
        private readonly Dictionary<ushort, MqttPublishPacket> _pendingExactlyOncePublishPackets = new Dictionary<ushort, MqttPublishPacket>();
        private readonly HashSet<ushort> _processedPublishPackets = new HashSet<ushort>();
        private readonly MqttPacketDispatcher _packetDispatcher = new MqttPacketDispatcher();
        private readonly MqttClientOptions _options;
        private readonly IMqttAdapter _adapter;

        private int _latestPacketIdentifier;
        private CancellationTokenSource _cancellationTokenSource;

        public MqttClient(MqttClientOptions options, IMqttAdapter adapter)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (adapter == null) throw new ArgumentNullException(nameof(adapter));

            _options = options;
            _adapter = adapter;
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
                KeepAlivePeriod = (ushort)_options.KeepAlivePeriod.TotalSeconds,
                WillMessage = willApplicationMessage
            };
            
            await _adapter.ConnectAsync(_options, _options.DefaultCommunicationTimeout);
            MqttTrace.Verbose(nameof(MqttClient), "Connection with server established.");

            _cancellationTokenSource = new CancellationTokenSource();
            _latestPacketIdentifier = 0;
            _processedPublishPackets.Clear();
            _packetDispatcher.Reset();
            IsConnected = true;

            Task.Factory.StartNew(async () => await ReceivePackets(
                    _cancellationTokenSource.Token), _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default).Forget();

            var response = await SendAndReceiveAsync<MqttConnAckPacket>(connectPacket, p => true);
            if (response.ConnectReturnCode != MqttConnectReturnCode.ConnectionAccepted)
            {
                throw new MqttConnectingFailedException(response.ConnectReturnCode);
            }

            if (_options.KeepAlivePeriod != TimeSpan.Zero)
            {
                Task.Factory.StartNew(async () => await SendKeepAliveMessagesAsync(
                    _cancellationTokenSource.Token), _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default).Forget();
            }

            Connected?.Invoke(this, EventArgs.Empty);
        }

        public async Task DisconnectAsync()
        {
            await SendAsync(new MqttDisconnectPacket());
            await DisconnectInternalAsync();
        }

        private void ThrowIfNotConnected()
        {
            if (!IsConnected)
            {
                throw new MqttCommunicationException("The client is not connected.");
            }
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

            Func<MqttSubAckPacket, bool> packetSelector = p => p.PacketIdentifier == subscribePacket.PacketIdentifier;
            var response = await SendAndReceiveAsync(subscribePacket, packetSelector);

            if (response.SubscribeReturnCodes.Count != topicFilters.Count)
            {
                throw new MqttProtocolViolationException("The return codes are not matching the topic filters [MQTT-3.9.3-1].");
            }

            var result = new List<MqttSubscribeResult>();
            for (var i = 0; i < topicFilters.Count; i++)
            {
                result.Add(new MqttSubscribeResult(topicFilters[i], response.SubscribeReturnCodes[i]));
            }

            return result;
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

            Func<MqttUnsubAckPacket, bool> packetSelector = p => p.PacketIdentifier == unsubscribePacket.PacketIdentifier;
            await SendAndReceiveAsync(unsubscribePacket, packetSelector);
        }

        public async Task PublishAsync(MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage == null) throw new ArgumentNullException(nameof(applicationMessage));
            ThrowIfNotConnected();

            var publishPacket = new MqttPublishPacket
            {
                Topic = applicationMessage.Topic,
                Payload = applicationMessage.Payload,
                QualityOfServiceLevel = applicationMessage.QualityOfServiceLevel,
                Retain = applicationMessage.Retain,
                Dup = false
            };

            if (publishPacket.QualityOfServiceLevel != MqttQualityOfServiceLevel.AtMostOnce)
            {
                publishPacket.PacketIdentifier = GetNewPacketIdentifier();
            }

            if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtLeastOnce)
            {
                if (!publishPacket.PacketIdentifier.HasValue) throw new InvalidOperationException();

                Func<MqttPubAckPacket, bool> packageSelector = p => p.PacketIdentifier == publishPacket.PacketIdentifier.Value;
                await SendAndReceiveAsync(publishPacket, packageSelector);
            }
            else if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.ExactlyOnce)
            {
                if (!publishPacket.PacketIdentifier.HasValue) throw new InvalidOperationException();

                Func<MqttPubRecPacket, bool> packageSelector = p => p.PacketIdentifier == publishPacket.PacketIdentifier.Value;
                await SendAndReceiveAsync(publishPacket, packageSelector);
                await SendAsync(new MqttPubCompPacket { PacketIdentifier = publishPacket.PacketIdentifier.Value });
            }
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
                _cancellationTokenSource?.Cancel();
                IsConnected = false;
                Disconnected?.Invoke(this, EventArgs.Empty);
            }
        }

        private async void ProcessIncomingPacket(MqttBasePacket mqttPacket)
        {
            var publishPacket = mqttPacket as MqttPublishPacket;
            if (publishPacket != null)
            {
                await ProcessReceivedPublishPacket(publishPacket);
                return;
            }

            var pingReqPacket = mqttPacket as MqttPingReqPacket;
            if (pingReqPacket != null)
            {
                await SendAsync(new MqttPingRespPacket());
                return;
            }

            var pubRelPacket = mqttPacket as MqttPubRelPacket;
            if (pubRelPacket != null)
            {
                await ProcessReceivedPubRelPacket(pubRelPacket);
                return;
            }

            _packetDispatcher.Dispatch(mqttPacket);
        }

        private void FireApplicationMessageReceivedEvent(MqttPublishPacket publishPacket)
        {
            if (publishPacket.QualityOfServiceLevel != MqttQualityOfServiceLevel.AtMostOnce)
            {
                if (publishPacket.PacketIdentifier == null) throw new InvalidOperationException();
                _processedPublishPackets.Add(publishPacket.PacketIdentifier.Value);
            }

            var applicationMessage = new MqttApplicationMessage(
                publishPacket.Topic,
                publishPacket.Payload,
                publishPacket.QualityOfServiceLevel,
                publishPacket.Retain
            );

            ApplicationMessageReceived?.Invoke(this, new MqttApplicationMessageReceivedEventArgs(applicationMessage));
        }

        private async Task ProcessReceivedPubRelPacket(MqttPubRelPacket pubRelPacket)
        {
            var originalPublishPacket = _pendingExactlyOncePublishPackets.Take(pubRelPacket.PacketIdentifier);
            if (originalPublishPacket == null) throw new MqttCommunicationException();
            await SendAsync(new MqttPubCompPacket { PacketIdentifier = pubRelPacket.PacketIdentifier });

            FireApplicationMessageReceivedEvent(originalPublishPacket);
        }

        private async Task ProcessReceivedPublishPacket(MqttPublishPacket publishPacket)
        {
            if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtMostOnce)
            {
                FireApplicationMessageReceivedEvent(publishPacket);
            }
            else
            {
                if (!publishPacket.PacketIdentifier.HasValue) { throw new InvalidOperationException(); }

                if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.AtLeastOnce)
                {
                    FireApplicationMessageReceivedEvent(publishPacket);
                    await SendAsync(new MqttPubAckPacket { PacketIdentifier = publishPacket.PacketIdentifier.Value });
                }
                else if (publishPacket.QualityOfServiceLevel == MqttQualityOfServiceLevel.ExactlyOnce)
                {
                    _pendingExactlyOncePublishPackets.Add(publishPacket.PacketIdentifier.Value, publishPacket);
                    await SendAsync(new MqttPubRecPacket { PacketIdentifier = publishPacket.PacketIdentifier.Value });
                }
            }
        }
        
        private async Task SendAsync(MqttBasePacket packet)
        {
            await _adapter.SendPacketAsync(packet, _options.DefaultCommunicationTimeout);
        }

        private async Task<TResponsePacket> SendAndReceiveAsync<TResponsePacket>(
            MqttBasePacket requestPacket, Func<TResponsePacket, bool> responsePacketSelector) where TResponsePacket : MqttBasePacket
        {
            Func<MqttBasePacket, bool> selector = p =>
            {
                var p1 = p as TResponsePacket;
                return p1 != null && responsePacketSelector(p1);
            };

            return (TResponsePacket)await SendAndReceiveAsync(requestPacket, selector);
        }

        private async Task<MqttBasePacket> SendAndReceiveAsync(MqttBasePacket requestPacket, Func<MqttBasePacket, bool> responsePacketSelector)
        {
            var waitTask = _packetDispatcher.WaitForPacketAsync(responsePacketSelector, _options.DefaultCommunicationTimeout);
            await _adapter.SendPacketAsync(requestPacket, _options.DefaultCommunicationTimeout);
            return await waitTask;
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
                    await SendAndReceiveAsync<MqttPingRespPacket>(new MqttPingReqPacket(), p => true);
                }
            }
            catch (Exception exception)
            {
                MqttTrace.Error(nameof(MqttClient), exception, "Error while sending keep alive packets.");
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
                    var mqttPacket = await _adapter.ReceivePacket();
                    MqttTrace.Information(nameof(MqttChannelAdapter), $"Received <<< {mqttPacket}");

                    Task.Run(() => ProcessIncomingPacket(mqttPacket), cancellationToken).Forget();
                }
            }
            catch (Exception exception)
            {
                MqttTrace.Error(nameof(MqttClient), exception, "Error while receiving packets.");
                await DisconnectInternalAsync();
            }
            finally
            {
                MqttTrace.Information(nameof(MqttClient), "Stopped receiving packets.");
            }
        }
    }
}