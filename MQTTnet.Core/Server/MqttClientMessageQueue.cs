using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Exceptions;
using MQTTnet.Core.Packets;

namespace MQTTnet.Core.Server
{
    public sealed class MqttClientMessageQueue
    {
        private readonly BlockingCollection<MqttClientPublishPacketContext> _pendingPublishPackets = new BlockingCollection<MqttClientPublishPacketContext>();

        private readonly MqttServerOptions _options;
        private CancellationTokenSource _cancellationTokenSource;
        private IMqttCommunicationAdapter _adapter;

        public MqttClientMessageQueue(MqttServerOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public void Start(IMqttCommunicationAdapter adapter)
        {
            if (_cancellationTokenSource != null)
            {
                throw new InvalidOperationException($"{nameof(MqttClientMessageQueue)} already started.");
            }

            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _cancellationTokenSource = new CancellationTokenSource();

            Task.Run(() => SendPendingPublishPacketsAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
        }

        public void Stop()
        {
            _adapter = null;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
            _pendingPublishPackets?.Dispose();
        }

        public void Enqueue(MqttPublishPacket publishPacket)
        {
            if (publishPacket == null) throw new ArgumentNullException(nameof(publishPacket));

            _pendingPublishPackets.Add(new MqttClientPublishPacketContext(publishPacket));
        }

        private async Task SendPendingPublishPacketsAsync(CancellationToken cancellationToken)
        {
            foreach (var publishPacket in _pendingPublishPackets.GetConsumingEnumerable(cancellationToken))
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    if (_adapter == null)
                    {
                        continue;
                    }

                    await TrySendPendingPublishPacketAsync(publishPacket).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    MqttTrace.Error(nameof(MqttClientMessageQueue), e, "Error while sending pending publish packets.");
                }
            }
        }

        private async Task TrySendPendingPublishPacketAsync(MqttClientPublishPacketContext publishPacketContext)
        {
            try
            {
                if (_adapter == null)
                {
                    return;
                }

                publishPacketContext.PublishPacket.Dup = publishPacketContext.SendTries > 0;
                await _adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, publishPacketContext.PublishPacket).ConfigureAwait(false);

                publishPacketContext.IsSent = true;
            }
            catch (MqttCommunicationException exception)
            {
                MqttTrace.Warning(nameof(MqttClientMessageQueue), exception, "Sending publish packet failed.");
                _pendingPublishPackets.Add(publishPacketContext);
            }
            catch (Exception exception)
            {
                MqttTrace.Error(nameof(MqttClientMessageQueue), exception, "Sending publish packet failed.");
                _pendingPublishPackets.Add(publishPacketContext);
            }
            finally
            {
                publishPacketContext.SendTries++;
            }
        }
    }
}
