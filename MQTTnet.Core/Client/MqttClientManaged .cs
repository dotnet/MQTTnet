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
    public class MqttClientManaged: IMqttClientManaged
    {
        private MqttClientManagedOptions _options;
        private int _latestPacketIdentifier;
        private readonly BlockingCollection<MqttApplicationMessage> _inflightQueue;
        private bool _usePersistance = false;
        private MqttClientQueuedPersistentMessagesManager _persistentMessagesManager;
        private readonly MqttClient _baseMqttClient;

        public MqttClientManaged(IMqttCommunicationAdapterFactory communicationChannelFactory)
        {
            _baseMqttClient = new MqttClient(communicationChannelFactory);
            _baseMqttClient.Connected += BaseMqttClient_Connected;
            _baseMqttClient.Disconnected += BaseMqttClient_Disconnected;
            _baseMqttClient.ApplicationMessageReceived += BaseMqttClient_ApplicationMessageReceived;
            _inflightQueue = new BlockingCollection<MqttApplicationMessage>();
        }

        private void BaseMqttClient_ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            ApplicationMessageReceived?.Invoke(this, e);
        }

        private void BaseMqttClient_Disconnected(object sender, EventArgs e)
        {
            Disconnected?.Invoke(this, e);
        }

        private void BaseMqttClient_Connected(object sender, EventArgs e)
        {
            Connected?.Invoke(this, e);
        }

        public event EventHandler Connected;
        public event EventHandler Disconnected;
        public event EventHandler<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;

        public bool IsConnected => _baseMqttClient.IsConnected;


        public async Task ConnectAsync(IMqttClientOptions options)
        {
            //TODO VERY BAD
            _options = options as MqttClientManagedOptions;
            this._usePersistance = _options.Storage != null;
            await _baseMqttClient.ConnectAsync(options);
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

        public async Task DisconnectAsync()
        {
            await _baseMqttClient.DisconnectAsync();
        }

        public async Task UnsubscribeAsync(IEnumerable<string> topicFilters)
        {
            await _baseMqttClient.UnsubscribeAsync(topicFilters);
        }

        public async Task PublishAsync(IEnumerable<MqttApplicationMessage> applicationMessages)
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

                _inflightQueue.Add(applicationMessage);
            }
        }

        public async Task<IList<MqttSubscribeResult>> SubscribeAsync(IEnumerable<TopicFilter> topicFilters)
        {
            return await _baseMqttClient.SubscribeAsync(topicFilters);
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
            Task.Factory.StartNew(
                () => SendPackets(_baseMqttClient._cancellationTokenSource.Token),
                _baseMqttClient._cancellationTokenSource.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).ConfigureAwait(false);
        }

        private async Task SendPackets(CancellationToken cancellationToken)
        {
            MqttNetTrace.Information(nameof(MqttClientManaged), "Start sending packets.");
            MqttApplicationMessage messageInQueue = null;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    messageInQueue = _inflightQueue.Take();
                    await _baseMqttClient.PublishAsync(new List<MqttApplicationMessage>() { messageInQueue });
                    if (_usePersistance)
                        await _persistentMessagesManager.Remove(messageInQueue);                   
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (MqttCommunicationException exception)
            {
                MqttNetTrace.Warning(nameof(MqttClient), exception, "MQTT communication exception while sending packets.");
                //message not send, equeue it again
                if (messageInQueue != null)
                    _inflightQueue.Add(messageInQueue);
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
