using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Exceptions;
using MQTTnet.Core.Packets;
using System.Linq;

namespace MQTTnet.Core.Server
{
    public sealed class MqttClientMessageQueue
    {
        private readonly BlockingCollection<MqttPublishPacket> _pendingPublishPackets = new BlockingCollection<MqttPublishPacket>();

        private readonly MqttServerOptions _options;
        private CancellationTokenSource _cancellationTokenSource;

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

            if (adapter == null) throw new ArgumentNullException(nameof(adapter));
            _cancellationTokenSource = new CancellationTokenSource();

            Task.Run(() => SendPendingPublishPacketsAsync(_cancellationTokenSource.Token, adapter), _cancellationTokenSource.Token);
        }

        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
            _pendingPublishPackets?.Dispose();
        }

        public void Enqueue(MqttPublishPacket publishPacket)
        {
            if (publishPacket == null) throw new ArgumentNullException(nameof(publishPacket));

            _pendingPublishPackets.Add(publishPacket);
        }

        private async Task SendPendingPublishPacketsAsync(CancellationToken cancellationToken, IMqttCommunicationAdapter adapter)
        {
            var consumable = _pendingPublishPackets.GetConsumingEnumerable();
            while (!cancellationToken.IsCancellationRequested)
            {
                var packets = consumable.Take(_pendingPublishPackets.Count).ToList();
                try
                {
                    await adapter.SendPacketsAsync(_options.DefaultCommunicationTimeout, packets).ConfigureAwait(false);
                }
                catch (MqttCommunicationException exception)
                {
                    MqttTrace.Warning(nameof(MqttClientMessageQueue), exception, "Sending publish packet failed.");
                    foreach (var publishPacket in packets)
                    {
                        publishPacket.Dup = true;
                        _pendingPublishPackets.Add(publishPacket);
                    }
                }
                catch (Exception exception)
                {
                    MqttTrace.Error(nameof(MqttClientMessageQueue), exception, "Sending publish packet failed.");
                    foreach (var publishPacket in packets)
                    {
                        publishPacket.Dup = true;
                        _pendingPublishPackets.Add(publishPacket);
                    }
                }
            }
        }
    }
}
