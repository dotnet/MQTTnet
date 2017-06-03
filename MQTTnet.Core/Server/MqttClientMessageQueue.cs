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

namespace MQTTnet.Core.Server
{
    public sealed class MqttClientMessageQueue
    {
        private readonly List<MqttClientPublishPacketContext> _pendingPublishPackets = new List<MqttClientPublishPacketContext>();
        private readonly AsyncGate _gate = new AsyncGate();

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

            Task.Run(() => SendPendingPublishPacketsAsync(_cancellationTokenSource.Token));
        }

        public void Stop()
        {
            _adapter = null;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
        }

        public void Enqueue(MqttClientSession senderClientSession, MqttPublishPacket publishPacket)
        {
            if (senderClientSession == null) throw new ArgumentNullException(nameof(senderClientSession));
            if (publishPacket == null) throw new ArgumentNullException(nameof(publishPacket));

            lock (_pendingPublishPackets)
            {
                _pendingPublishPackets.Add(new MqttClientPublishPacketContext(senderClientSession, publishPacket));
                _gate.Set();
            }
        }

        private async Task SendPendingPublishPacketsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await _gate.WaitOneAsync();
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    if (_adapter == null)
                    {
                        continue;
                    }

                    List<MqttClientPublishPacketContext> pendingPublishPackets;
                    lock (_pendingPublishPackets)
                    {
                        pendingPublishPackets = _pendingPublishPackets.ToList();
                    }

                    foreach (var publishPacket in pendingPublishPackets)
                    {
                        await TrySendPendingPublishPacketAsync(publishPacket);
                    }
                }
                catch (Exception e)
                {
                    MqttTrace.Error(nameof(MqttClientMessageQueue), e, "Error while sending pending publish packets.");
                }
                finally
                {
                    Cleanup();
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
                await _adapter.SendPacketAsync(publishPacketContext.PublishPacket, _options.DefaultCommunicationTimeout);

                publishPacketContext.IsSent = true;
            }
            catch (MqttCommunicationException exception)
            {
                MqttTrace.Warning(nameof(MqttClientMessageQueue), exception, "Sending publish packet failed.");
            }
            catch (Exception exception)
            {
                MqttTrace.Error(nameof(MqttClientMessageQueue), exception, "Sending publish packet failed.");
            }
            finally
            {
                publishPacketContext.SendTries++;
            }
        }

        private void Cleanup()
        {
            lock (_pendingPublishPackets)
            {
                _pendingPublishPackets.RemoveAll(p => p.IsSent);
            }
        }
    }
}
