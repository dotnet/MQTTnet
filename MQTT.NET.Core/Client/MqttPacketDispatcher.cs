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
        private readonly List<MqttPacketAwaiter> _packetAwaiters = new List<MqttPacketAwaiter>();

        public async Task<MqttBasePacket> WaitForPacketAsync(Func<MqttBasePacket, bool> selector, TimeSpan timeout)
        {
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            var waitHandle = new MqttPacketAwaiter(selector);
            AddPacketAwaiter(waitHandle);

            var hasTimeout = await Task.WhenAny(Task.Delay(timeout), waitHandle.Task) != waitHandle.Task;
            RemovePacketAwaiter(waitHandle);

            if (hasTimeout)
            {
                MqttTrace.Error(nameof(MqttPacketDispatcher), $"Timeout while waiting for packet.");
                throw new MqttCommunicationTimedOutException();
            }

            return waitHandle.Task.Result;
        }

        public void Dispatch(MqttBasePacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

            var packetDispatched = false;
            foreach (var packetAwaiter in GetPacketAwaiters())
            {
                if (packetAwaiter.CheckPacket(packet))
                {
                    packetDispatched = true;
                }
            }

            if (!packetDispatched)
            {
                MqttTrace.Warning(nameof(MqttPacketDispatcher), $"Received packet '{packet}' not dispatched.");
            }
        }

        private List<MqttPacketAwaiter> GetPacketAwaiters()
        {
            lock (_packetAwaiters)
            {
                return new List<MqttPacketAwaiter>(_packetAwaiters);
            }
        }

        private void AddPacketAwaiter(MqttPacketAwaiter packetAwaiter)
        {
            lock (_packetAwaiters)
            {
                _packetAwaiters.Add(packetAwaiter);
            }
        }

        private void RemovePacketAwaiter(MqttPacketAwaiter packetAwaiter)
        {
            lock (_packetAwaiters)
            {
                _packetAwaiters.Remove(packetAwaiter);
            }
        }

        public void Reset()
        {
            lock (_packetAwaiters)
            {
                _packetAwaiters.Clear();
            }
        }
    }
}