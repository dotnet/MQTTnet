using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Exceptions;
using MQTTnet.Core.Internal;
using MQTTnet.Core.Packets;
using System.Collections.Concurrent;

namespace MQTTnet.Core.Client
{
    public class MqttPacketDispatcher
    {
        private readonly object _syncRoot = new object();
        private readonly HashSet<MqttBasePacket> _receivedPackets = new HashSet<MqttBasePacket>();
        private readonly ConcurrentDictionary<Type, TaskCompletionSource<MqttBasePacket>> _packetByResponseType = new ConcurrentDictionary<Type, TaskCompletionSource<MqttBasePacket>>();
        private readonly ConcurrentDictionary<ushort,TaskCompletionSource<MqttBasePacket>> _packetByIdentifier = new ConcurrentDictionary<ushort, TaskCompletionSource<MqttBasePacket>>();

        public async Task<MqttBasePacket> WaitForPacketAsync(MqttBasePacket request, Type responseType, TimeSpan timeout)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var packetAwaiter = AddPacketAwaiter(request, responseType);
            DispatchPendingPackets();

            try
            {
                return await packetAwaiter.Task.TimeoutAfter( timeout );
            }
            catch ( MqttCommunicationTimedOutException )
            {
                MqttTrace.Warning( nameof( MqttPacketDispatcher ), "Timeout while waiting for packet." );
                throw;
            }
            finally
            {
                RemovePacketAwaiter(request, responseType);
            }
        }

        public void Dispatch(MqttBasePacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

            var packetDispatched = false;

            if (packet is IMqttPacketWithIdentifier withIdentifier)
            {
                if (_packetByIdentifier.TryRemove(withIdentifier.PacketIdentifier, out var tcs))
                {
                    tcs.TrySetResult(packet);
                    packetDispatched = true;
                }
            }
            else if (_packetByResponseType.TryRemove(packet.GetType(), out var tcs) )
            {
                tcs.TrySetResult( packet);
                packetDispatched = true;
            }

            lock (_syncRoot)
            {
                if (!packetDispatched)
                {
                    _receivedPackets.Add(packet);
                }
                else
                {
                    _receivedPackets.Remove(packet);
                }
            }
        }

        public void Reset()
        {
            lock (_syncRoot)
            {
                _receivedPackets.Clear();
            }

            _packetByIdentifier.Clear();
        }

        private TaskCompletionSource<MqttBasePacket> AddPacketAwaiter(MqttBasePacket request, Type responseType)
        {
            var tcs = new TaskCompletionSource<MqttBasePacket>();
            if (request is IMqttPacketWithIdentifier withIdent)
            {
                _packetByIdentifier[withIdent.PacketIdentifier] = tcs;
            }
            else
            {
                _packetByResponseType[responseType] = tcs;
            }

            return tcs;
        }

        private void RemovePacketAwaiter(MqttBasePacket request, Type responseType)
        {
            if (request is IMqttPacketWithIdentifier withIdent)
            {
                _packetByIdentifier.TryRemove(withIdent.PacketIdentifier, out var tcs);
            }
            else
            {
                _packetByResponseType.TryRemove(responseType, out var tcs);
            }
        }

        private void DispatchPendingPackets()
        {
            List<MqttBasePacket> receivedPackets;
            lock (_syncRoot)
            {
                receivedPackets = new List<MqttBasePacket>(_receivedPackets);
            }

            foreach (var pendingPacket in receivedPackets)
            {
                Dispatch(pendingPacket);
            }
        }
    }
}