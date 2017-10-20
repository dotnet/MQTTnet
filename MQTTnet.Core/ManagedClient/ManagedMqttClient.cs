using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Core.Client;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Exceptions;
using MQTTnet.Core.Packets;

namespace MQTTnet.Core.ManagedClient
{
    public class ManagedMqttClient
    {
        private readonly List<MqttApplicationMessage> _messageQueue = new List<MqttApplicationMessage>();
        private readonly AutoResetEvent _messageQueueGate = new AutoResetEvent(false);
        private readonly MqttClient _mqttClient;
        private readonly MqttNetTrace _trace;

        private IManagedMqttClientOptions _options;
        
        public ManagedMqttClient(IMqttCommunicationAdapterFactory communicationChannelFactory, MqttNetTrace trace)
        {
            if (communicationChannelFactory == null) throw new ArgumentNullException(nameof(communicationChannelFactory));
            _trace = trace ?? throw new ArgumentNullException(nameof(trace));

            _mqttClient = new MqttClient(communicationChannelFactory, _trace);
            _mqttClient.Connected += OnConnected;
            _mqttClient.Disconnected += OnDisconnected;
            _mqttClient.ApplicationMessageReceived += OnApplicationMessageReceived;
        }

        public event EventHandler Connected;
        public event EventHandler<MqttClientDisconnectedEventArgs> Disconnected;
        public event EventHandler<MqttApplicationMessageReceivedEventArgs> ApplicationMessageReceived;

        public bool IsConnected => _mqttClient.IsConnected;


        public void Start(IManagedMqttClientOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (options.ClientOptions == null) throw new ArgumentException("The client options are not set.", nameof(options));


        }

        public void Stop()
        {
            
        }

        public async Task ConnectAsync(IManagedMqttClientOptions options)
        {
            //////TODO VERY BAD
            ////_options = options as ManagedMqttClientTcpOptions;
            ////this._usePersistance = _options.Storage != null;
            ////await _mqttClient.ConnectAsync(options);
            ////SetupOutgoingPacketProcessingAsync();

            //////load persistentMessages
            ////////if (_usePersistance)
            ////////{
            ////////    if (_persistentMessagesManager == null)
            ////////        _persistentMessagesManager = new ManagedMqttClientMessagesManager(_options);
            ////////    await _persistentMessagesManager.LoadMessagesAsync();
            ////////    await InternalPublishAsync(_persistentMessagesManager.GetMessages(), false);
            ////////}
        }

        public async Task DisconnectAsync()
        {
            await _mqttClient.DisconnectAsync();
        }

        public async Task UnsubscribeAsync(IEnumerable<string> topicFilters)
        {
            // TODO: Move all subscriptions to list an subscribe after connection has lost. But only if server session is new.
            await _mqttClient.UnsubscribeAsync(topicFilters);
        }

        public void Enqueue(IEnumerable<MqttApplicationMessage> applicationMessages)
        {
            if (applicationMessages == null) throw new ArgumentNullException(nameof(applicationMessages));

            ThrowIfNotConnected();

            _messageQueue.AddRange(applicationMessages);
            _options.Storage?.SaveQueuedMessagesAsync(_messageQueue.ToList());

            _messageQueueGate.Set();
        }
        
        public async Task<IList<MqttSubscribeResult>> SubscribeAsync(IEnumerable<TopicFilter> topicFilters)
        {
            return await _mqttClient.SubscribeAsync(topicFilters);
        }

        private void ThrowIfNotConnected()
        {
            if (!IsConnected) throw new MqttCommunicationException("The client is not connected.");
        }       

        private void SetupOutgoingPacketProcessingAsync()
        {
            //Task.Factory.StartNew(
            //    () => SendPackets(_mqttClient._cancellationTokenSource.Token),
            //    _mqttClient._cancellationTokenSource.Token,
            //    TaskCreationOptions.LongRunning,
            //    TaskScheduler.Default).ConfigureAwait(false);
        }

        private async Task SendPackets(CancellationToken cancellationToken)
        {
            //MqttNetTrace.Information(nameof(MqttClientManaged), "Start sending packets.");
            //MqttApplicationMessage messageInQueue = null;

            //try
            //{
            //    while (!cancellationToken.IsCancellationRequested)
            //    {
            //        messageInQueue = _inflightQueue.Take();
            //        await _mqttClient.PublishAsync(new List<MqttApplicationMessage>() { messageInQueue });
            //        if (_usePersistance)
            //            await _persistentMessagesManager.Remove(messageInQueue);                   
            //    }
            //}
            //catch (OperationCanceledException)
            //{
            //}
            //catch (MqttCommunicationException exception)
            //{
            //    MqttNetTrace.Warning(nameof(MqttClient), exception, "MQTT communication exception while sending packets.");
            //    //message not send, equeue it again
            //    if (messageInQueue != null)
            //        _inflightQueue.Add(messageInQueue);
            //}
            //catch (Exception exception)
            //{
            //    MqttNetTrace.Error(nameof(MqttClient), exception, "Unhandled exception while sending packets.");
            //    await DisconnectAsync().ConfigureAwait(false);
            //}
            //finally
            //{
            //    MqttNetTrace.Information(nameof(MqttClient), "Stopped sending packets.");
            //}
        }

        private void OnApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            ApplicationMessageReceived?.Invoke(this, eventArgs);
        }

        private void OnDisconnected(object sender, MqttClientDisconnectedEventArgs eventArgs)
        {
            Disconnected?.Invoke(this, eventArgs);
        }

        private void OnConnected(object sender, EventArgs eventArgs)
        {
            Connected?.Invoke(this, eventArgs);
        }
    }
}
