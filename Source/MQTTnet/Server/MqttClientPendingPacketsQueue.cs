using System;
using System.Collections.Generic;
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
    public class MqttClientPendingPacketsQueue : AsyncQueue<MqttBasePacket>
    {
        private readonly MqttClientSession _clientSession;
        private readonly IMqttNetChildLogger _logger;

        public MqttClientPendingPacketsQueue(IMqttServerOptions options, MqttClientSession clientSession, IMqttNetChildLogger logger)
            : base(options)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            _clientSession = clientSession ?? throw new ArgumentNullException(nameof(clientSession));

            _logger = logger.CreateChildLogger(nameof(MqttClientPendingPacketsQueue));
        }
        
        public void Start(IMqttChannelAdapter adapter, CancellationToken cancellationToken)
        {
            if (adapter == null) throw new ArgumentNullException(nameof(adapter));

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            Task.Run(() => SendQueuedPacketsAsync(adapter, cancellationToken), cancellationToken);
        }

        protected override bool TryEnqueue(MqttBasePacket packet)
        {
            var didEnqueue = base.TryEnqueue(packet);
            if (didEnqueue)
            {
                _logger.Verbose("Enqueued packet (ClientId: {0}).", _clientSession.ClientId);
            }
            return didEnqueue;
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
                if (!TryDequeue(out packet))
                {
                    packet = await DequeueAsync(cancellationToken).ConfigureAwait(false);
                }

                if (packet == null || cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                adapter.SendPacketAsync(packet, cancellationToken).GetAwaiter().GetResult();

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
