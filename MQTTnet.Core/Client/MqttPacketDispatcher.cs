using System;
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
        private readonly ConcurrentDictionary<Type, TaskCompletionSource<MqttBasePacket>> _packetByResponseType = new ConcurrentDictionary<Type, TaskCompletionSource<MqttBasePacket>>();
        private readonly ConcurrentDictionary<Type, ConcurrentDictionary<ushort, TaskCompletionSource<MqttBasePacket>>> _packetByResponseTypeAndIdentifier = new ConcurrentDictionary<Type, ConcurrentDictionary<ushort, TaskCompletionSource<MqttBasePacket>>>();

        public async Task<MqttBasePacket> WaitForPacketAsync(MqttBasePacket request, Type responseType, TimeSpan timeout)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var packetAwaiter = AddPacketAwaiter(request, responseType);
            try
            {
                return await packetAwaiter.Task.TimeoutAfter(timeout).ConfigureAwait(false);
            }
            catch (MqttCommunicationTimedOutException)
            {
                MqttTrace.Warning(nameof(MqttPacketDispatcher), "Timeout while waiting for packet of type '{0}'.", responseType.Name);
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

            var type = packet.GetType();
            if (packet is IMqttPacketWithIdentifier withIdentifier)
            {
                if (_packetByResponseTypeAndIdentifier.TryGetValue(type, out var byid))
                {
                    if (byid.TryRemove(withIdentifier.PacketIdentifier, out var tcs))
                    {
                        tcs.TrySetResult(packet);
                        return;
                    }
                }
            }
            else if (_packetByResponseType.TryRemove(type, out var tcs))
            {
                tcs.TrySetResult(packet);
                return;
            }

            throw new InvalidOperationException($"Packet of type '{type.Name}' not handled or dispatched.");
        }

        public void Reset()
        {
            _packetByResponseTypeAndIdentifier.Clear();
            _packetByResponseType.Clear();
        }

        private TaskCompletionSource<MqttBasePacket> AddPacketAwaiter(MqttBasePacket request, Type responseType)
        {
            var tcs = new TaskCompletionSource<MqttBasePacket>();

            if (request is IMqttPacketWithIdentifier requestWithIdentifier)
            {
                var byId = _packetByResponseTypeAndIdentifier.GetOrAdd(responseType, key => new ConcurrentDictionary<ushort, TaskCompletionSource<MqttBasePacket>>());
                byId[requestWithIdentifier.PacketIdentifier] = tcs;
            }
            else
            {
                _packetByResponseType[responseType] = tcs;
            }

            return tcs;
        }

        private void RemovePacketAwaiter(MqttBasePacket request, Type responseType)
        {
            if (request is IMqttPacketWithIdentifier requestWithIdentifier)
            {
                var byId = _packetByResponseTypeAndIdentifier.GetOrAdd(responseType, key => new ConcurrentDictionary<ushort, TaskCompletionSource<MqttBasePacket>>());
                byId.TryRemove(requestWithIdentifier.PacketIdentifier, out var _);
            }
            else
            {
                _packetByResponseType.TryRemove(responseType, out var _);
            }
        }
    }
}