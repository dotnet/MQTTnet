using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet.Core.Diagnostics;
using MQTTnet.Core.Exceptions;
using MQTTnet.Core.Packets;

namespace MQTTnet.Core.Client
{
    public class MqttPacketDispatcher
    {
        private readonly object _syncRoot = new object();
        private readonly HashSet<MqttBasePacket> _receivedPackets = new HashSet<MqttBasePacket>();
        private readonly List<MqttPacketAwaiter> _packetAwaiters = new List<MqttPacketAwaiter>();

        public async Task<MqttBasePacket> WaitForPacketAsync(Func<MqttBasePacket, bool> selector, TimeSpan timeout)
        {
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            var packetAwaiter = AddPacketAwaiter(selector);
            DispatchPendingPackets();

            var hasTimeout = await Task.WhenAny(Task.Delay(timeout), packetAwaiter.Task).ConfigureAwait(false) != packetAwaiter.Task;
            RemovePacketAwaiter(packetAwaiter);

            if (hasTimeout)
            {
                MqttTrace.Warning(nameof(MqttPacketDispatcher), "Timeout while waiting for packet.");
                throw new MqttCommunicationTimedOutException();
            }

            return packetAwaiter.Task.Result;
        }

        public void Dispatch(MqttBasePacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

            var packetDispatched = false;
            foreach (var packetAwaiter in GetPacketAwaiters())
            {
                if (packetAwaiter.PacketSelector(packet))
                {
                    packetAwaiter.TrySetResult(packet);
                    packetDispatched = true;
                    break;
                }
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
                _packetAwaiters.Clear();
                _receivedPackets.Clear();
            }
        }

        private List<MqttPacketAwaiter> GetPacketAwaiters()
        {
            lock (_syncRoot)
            {
                return new List<MqttPacketAwaiter>(_packetAwaiters);
            }
        }

        private MqttPacketAwaiter AddPacketAwaiter(Func<MqttBasePacket, bool> selector)
        {
            lock (_syncRoot)
            {
                var packetAwaiter = new MqttPacketAwaiter(selector);
                _packetAwaiters.Add(packetAwaiter);
                return packetAwaiter;
            }
        }

        private void RemovePacketAwaiter(MqttPacketAwaiter packetAwaiter)
        {
            lock (_syncRoot)
            {
                _packetAwaiters.Remove(packetAwaiter);
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