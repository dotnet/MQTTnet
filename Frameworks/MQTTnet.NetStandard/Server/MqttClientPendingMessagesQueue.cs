using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Internal;
using MQTTnet.Packets;
using MQTTnet.Protocol;

namespace MQTTnet.Server
{
    public sealed class MqttClientPendingMessagesQueue : IDisposable
    {
        private readonly AsyncAutoResetEvent _queueAutoResetEvent = new AsyncAutoResetEvent();
        private readonly IMqttServerOptions _options;
        private readonly MqttClientSession _clientSession;
        private readonly IMqttNetChildLogger _logger;

        private ConcurrentQueue<MqttBasePacket> _queue = new ConcurrentQueue<MqttBasePacket>();
        private Task _workerTask;

        public MqttClientPendingMessagesQueue(IMqttServerOptions options, MqttClientSession clientSession, IMqttNetChildLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _clientSession = clientSession ?? throw new ArgumentNullException(nameof(clientSession));

            _logger = logger.CreateChildLogger(nameof(MqttClientPendingMessagesQueue));
        }

        public int Count => _queue.Count;

        public void Start(IMqttChannelAdapter adapter, CancellationToken cancellationToken)
        {
            if (adapter == null) throw new ArgumentNullException(nameof(adapter));

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            _workerTask = Task.Run(() => SendQueuedPacketsAsync(adapter, cancellationToken), cancellationToken);
        }
        
        public void Enqueue(MqttBasePacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

            if (_queue.Count >= _options.MaxPendingMessagesPerClient)
            {
                if (_options.PendingMessagesOverflowStrategy == MqttPendingMessagesOverflowStrategy.DropNewMessage)
                {
                    return;
                }

                if (_options.PendingMessagesOverflowStrategy == MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage)
                {
                    _queue.TryDequeue(out _);
                }
            }

            _queue.Enqueue(packet);
            _queueAutoResetEvent.Set();

            _logger.Verbose("Enqueued packet (ClientId: {0}).", _clientSession.ClientId);
        }

        public void Clear()
        {
            var newQueue = new ConcurrentQueue<MqttBasePacket>();
            Interlocked.Exchange(ref _queue, newQueue);
        }

        public void Dispose()
        {
            _queueAutoResetEvent?.Dispose();
        }

        private async Task SendQueuedPacketsAsync(IMqttChannelAdapter adapter, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await TrySendNextQueuedPacketAsync(adapter, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Unhandled exception while sending enqueued packet (ClientId: {0}).", _clientSession.ClientId);
            }
        }

        private async Task TrySendNextQueuedPacketAsync(IMqttChannelAdapter adapter, CancellationToken cancellationToken)
        {
            MqttBasePacket packet = null;
            try
            {
                if (_queue.IsEmpty)
                {
                    await _queueAutoResetEvent.WaitOneAsync(cancellationToken).ConfigureAwait(false);
                }

                if (!_queue.TryDequeue(out packet))
                {
                    return;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, new[] { packet }, cancellationToken).ConfigureAwait(false);

                _logger.Verbose("Enqueued packet sent (ClientId: {0}).", _clientSession.ClientId);
            }
            catch (Exception exception)
            {
                if (exception is MqttCommunicationTimedOutException)
                {
                    _logger.Warning(exception, "Sending publish packet failed: Timeout (ClientId: {0}).", _clientSession.ClientId);
                }
                else if (exception is MqttCommunicationException)
                {
                    _logger.Warning(exception, "Sending publish packet failed: Communication exception (ClientId: {0}).", _clientSession.ClientId);
                }
                else if (exception is OperationCanceledException)
                {
                }
                else
                {
                    _logger.Error(exception, "Sending publish packet failed (ClientId: {0}).", _clientSession.ClientId);
                }

                if (packet is MqttPublishPacket publishPacket)
                {
                    if (publishPacket.QualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce)
                    {
                        publishPacket.Dup = true;

                        Enqueue(publishPacket);
                    }
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    _clientSession.Stop(MqttClientDisconnectType.NotClean);
                }
            }
        }
    }
}
