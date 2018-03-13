using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using MQTTnet.Diagnostics;
using MQTTnet.Exceptions;
using MQTTnet.Internal;
using MQTTnet.Packets;

namespace MQTTnet.Client
{
    public class MqttPacketDispatcher
    {
        private readonly ConcurrentDictionary<Type, ConcurrentDictionary<ushort, TaskCompletionSource<MqttBasePacket>>> _awaiters = new ConcurrentDictionary<Type, ConcurrentDictionary<ushort, TaskCompletionSource<MqttBasePacket>>>();
        private readonly IMqttNetLogger _logger;

        public MqttPacketDispatcher(IMqttNetLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<MqttBasePacket> WaitForPacketAsync(Type responseType, ushort identifier, TimeSpan timeout)
        {
            var packetAwaiter = AddPacketAwaiter(responseType, identifier);
            try
            {
                return await packetAwaiter.Task.TimeoutAfter(timeout).ConfigureAwait(false);
            }
            catch (MqttCommunicationTimedOutException)
            {
                _logger.Warning<MqttPacketDispatcher>("Timeout while waiting for packet of type '{0}'.", responseType.Name);
                throw;
            }
            finally
            {
                RemovePacketAwaiter(responseType, identifier);
            }
        }

        public void Dispatch(MqttBasePacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

            var type = packet.GetType();

            if (_awaiters.TryGetValue(type, out var byId))
            {
                ushort identifier = 0;
                if (packet is IMqttPacketWithIdentifier packetWithIdentifier)
                {
                    identifier = packetWithIdentifier.PacketIdentifier;
                }

                if (byId.TryRemove(identifier, out var tcs))
                {
                    tcs.TrySetResult(packet);
                    return;
                }
            }

            throw new InvalidOperationException($"Packet of type '{type.Name}' not handled or dispatched.");
        }

        public void Reset()
        {
            _awaiters.Clear();
        }

        private TaskCompletionSource<MqttBasePacket> AddPacketAwaiter(Type responseType, ushort identifier)
        {
            var tcs = new TaskCompletionSource<MqttBasePacket>();

            var byId = _awaiters.GetOrAdd(responseType, key => new ConcurrentDictionary<ushort, TaskCompletionSource<MqttBasePacket>>());
            if (!byId.TryAdd(identifier, tcs))
            {
                throw new InvalidOperationException($"The packet dispatcher already has an awaiter for packet of type '{responseType}' with identifier {identifier}.");
            }

            return tcs;
        }

        private void RemovePacketAwaiter(Type responseType, ushort identifier)
        {
            var byId = _awaiters.GetOrAdd(responseType, key => new ConcurrentDictionary<ushort, TaskCompletionSource<MqttBasePacket>>());
            byId.TryRemove(identifier, out var _);
        }
    }
}