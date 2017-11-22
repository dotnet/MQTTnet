using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Exceptions;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;

namespace MQTTnet.Core.Server
{
    public sealed class MqttClientPendingMessagesQueue
    {
        private readonly BlockingCollection<MqttPublishPacket> _pendingPublishPackets = new BlockingCollection<MqttPublishPacket>();
        private readonly MqttServerOptions _options;
        private readonly MqttClientSession _session;
        private readonly IMqttNetLogger _logger;

        public MqttClientPendingMessagesQueue(MqttServerOptions options, MqttClientSession session, IMqttNetLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public void Start(IMqttChannelAdapter adapter, CancellationToken cancellationToken)
        {
            if (adapter == null) throw new ArgumentNullException(nameof(adapter));

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            Task.Run(async () => await SendPendingPublishPacketsAsync(adapter, cancellationToken), cancellationToken).ConfigureAwait(false);
        }

        public void Enqueue(MqttPublishPacket publishPacket)
        {
            if (publishPacket == null) throw new ArgumentNullException(nameof(publishPacket));

            _pendingPublishPackets.Add(publishPacket);
            _logger.Trace<MqttClientPendingMessagesQueue>("Enqueued packet (ClientId: {0}).", _session.ClientId);
        }

        private async Task SendPendingPublishPacketsAsync(IMqttChannelAdapter adapter, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await SendPendingPublishPacketAsync(adapter, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.Error<MqttClientPendingMessagesQueue>(exception, "Unhandled exception while sending enqueued packet (ClientId: {0}).", _session.ClientId);
            }
        }

        private async Task SendPendingPublishPacketAsync(IMqttChannelAdapter adapter, CancellationToken cancellationToken)
        {
            MqttPublishPacket packet = null;
            try
            {
                packet = _pendingPublishPackets.Take(cancellationToken);
                await adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, cancellationToken, packet).ConfigureAwait(false);

                _logger.Trace<MqttClientPendingMessagesQueue>("Enqueued packet sent (ClientId: {0}).", _session.ClientId);
            }
            catch (Exception exception)
            {
                if (exception is MqttCommunicationTimedOutException)
                {
                    _logger.Warning<MqttClientPendingMessagesQueue>(exception, "Sending publish packet failed due to timeout (ClientId: {0}).", _session.ClientId);
                }
                else if (exception is MqttCommunicationException)
                {
                    _logger.Warning<MqttClientPendingMessagesQueue>(exception, "Sending publish packet failed due to communication exception (ClientId: {0}).", _session.ClientId);
                }
                else if (exception is OperationCanceledException)
                {
                }
                else
                {
                    _logger.Error<MqttClientPendingMessagesQueue>(exception, "Sending publish packet failed (ClientId: {0}).", _session.ClientId);
                }

                if (packet != null && packet.QualityOfServiceLevel > MqttQualityOfServiceLevel.AtMostOnce)
                {
                    packet.Dup = true;
                    _pendingPublishPackets.Add(packet, CancellationToken.None);
                }

                await _session.StopAsync();
            }
        }
    }
}
