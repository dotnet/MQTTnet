using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Exceptions;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;
using MQTTnet.Core.Internal;

namespace MQTTnet.Core.Client
{
    public class MqttClientQueued : MqttClient, IMqttClientQueued
    {
        private MqttClientQueuedOptions _options;
        private int _latestPacketIdentifier;
        private readonly ConcurrentQueue<MqttApplicationMessage> _inflightQueue;
        private bool _usePersistance = false;
        private MqttClientQueuedPersistentMessagesManager _persistentMessagesManager;

        public MqttClientQueued(IMqttCommunicationAdapterFactory communicationChannelFactory) : base(communicationChannelFactory)
        {
            _inflightQueue = new ConcurrentQueue<MqttApplicationMessage>();
        }


        public async Task ConnectAsync(MqttClientQueuedOptions options)
        {
            try
            {
                _options = options;
                this._usePersistance = _options.UsePersistence;
                await base.ConnectAsync(options);
                SetupOutgoingPacketProcessingAsync();

                //load persistentMessages
                if (_usePersistance)
                {
                    if (_persistentMessagesManager == null)
                        _persistentMessagesManager = new MqttClientQueuedPersistentMessagesManager(_options);
                    await _persistentMessagesManager.LoadMessagesAsync();
                    await InternalPublishAsync(_persistentMessagesManager.GetMessages(), false);
                }
            }
            catch (Exception)
            {
                await DisconnectAsync().ConfigureAwait(false);
                throw;
            }
        }

        public new async Task PublishAsync(IEnumerable<MqttApplicationMessage> applicationMessages)
        {
            await InternalPublishAsync(applicationMessages, true);
        }

        private async Task InternalPublishAsync(IEnumerable<MqttApplicationMessage> applicationMessages, bool appendIfUsePersistance)
        {
            ThrowIfNotConnected();

            foreach (var applicationMessage in applicationMessages)
            {
                if (_usePersistance && appendIfUsePersistance)
                    await _persistentMessagesManager.SaveMessageAsync(applicationMessage);

                _inflightQueue.Enqueue(applicationMessage);
            }
        }

        public new async Task<IList<MqttSubscribeResult>> SubscribeAsync(IEnumerable<TopicFilter> topicFilters)
        {
            return await base.SubscribeAsync(topicFilters);
        }

        private void ThrowIfNotConnected()
        {
            if (!IsConnected) throw new MqttCommunicationException("The client is not connected.");
        }

        private ushort GetNewPacketIdentifier()
        {
            return (ushort)Interlocked.Increment(ref _latestPacketIdentifier);
        }
        

        private void SetupOutgoingPacketProcessingAsync()
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Factory.StartNew(
                () => SendPackets(base._cancellationTokenSource.Token),
                base._cancellationTokenSource.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).ConfigureAwait(false);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        private async Task SendPackets(CancellationToken cancellationToken)
        {
            MqttNetTrace.Information(nameof(MqttClient), "Start sending packets.");
            MqttApplicationMessage messageToSend = null;
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    while (_inflightQueue.TryDequeue(out messageToSend))
                    {
                        MqttQualityOfServiceLevel qosGroup = messageToSend.QualityOfServiceLevel;
                        MqttPublishPacket publishPacket = messageToSend.ToPublishPacket();
                        switch (qosGroup)
                        {                           
                            case MqttQualityOfServiceLevel.AtMostOnce:
                                {
                                    // No packet identifier is used for QoS 0 [3.3.2.2 Packet Identifier]
                                    await base._adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, base._cancellationTokenSource.Token, publishPacket);
                                    break;
                                }

                            case MqttQualityOfServiceLevel.AtLeastOnce:
                                {
                                    publishPacket.PacketIdentifier = GetNewPacketIdentifier();
                                    await base.SendAndReceiveAsync<MqttPubAckPacket>(publishPacket);
                                    break;
                                }
                            case MqttQualityOfServiceLevel.ExactlyOnce:
                                {
                                    publishPacket.PacketIdentifier = GetNewPacketIdentifier();
                                    var pubRecPacket = await base.SendAndReceiveAsync<MqttPubRecPacket>(publishPacket).ConfigureAwait(false);
                                    await base.SendAndReceiveAsync<MqttPubCompPacket>(pubRecPacket.CreateResponse<MqttPubRelPacket>()).ConfigureAwait(false);
                                    break;
                                }
                            default:
                                {
                                    throw new InvalidOperationException();
                                }
                        }
                        //delete from persistence
                        if (_usePersistance)
                            await _persistentMessagesManager.Remove(messageToSend);
                    }
                };
            }
            catch (OperationCanceledException)
            {
            }
            catch (MqttCommunicationException exception)
            {
                MqttNetTrace.Warning(nameof(MqttClient), exception, "MQTT communication exception while sending packets.");
                //message not send, equeued again
                if (messageToSend != null)
                    _inflightQueue.Enqueue(messageToSend);
            }
            catch (Exception exception)
            {
                MqttNetTrace.Error(nameof(MqttClient), exception, "Unhandled exception while sending packets.");
                await DisconnectAsync().ConfigureAwait(false);
            }
            finally
            {
                MqttNetTrace.Information(nameof(MqttClient), "Stopped sending packets.");
            }
        }
    }
}
