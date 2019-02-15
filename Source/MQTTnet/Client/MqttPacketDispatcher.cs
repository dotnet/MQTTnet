using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using MQTTnet.Packets;

namespace MQTTnet.Client
{
    public class MqttPacketDispatcher
    {
        private readonly ConcurrentDictionary<Tuple<ushort, Type>, TaskCompletionSource<MqttBasePacket>> _awaiters = new ConcurrentDictionary<Tuple<ushort, Type>, TaskCompletionSource<MqttBasePacket>>();

        public async Task Dispatch(Exception exception)
        {
            foreach (var awaiter in _awaiters)
            {
                awaiter.Value.TrySetException(exception);
            }

            // await the tasks to complete
            try
            {
                await Task.WhenAll(_awaiters.Values.Select(t => t.Task)).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // swallow any exception that happens during exception handling to prevent
                // exceptions bubbling into the internals of the MqttClient implementation
            }

            Reset();
        }

        public Task Dispatch(MqttBasePacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

            ushort identifier = 0;
            if (packet is IMqttPacketWithIdentifier packetWithIdentifier && packetWithIdentifier.PacketIdentifier.HasValue)
            {
                identifier = packetWithIdentifier.PacketIdentifier.Value;
            }

            var type = packet.GetType();
            var key = new Tuple<ushort, Type>(identifier, type);

            if (_awaiters.TryRemove(key, out var awaiter))
            {
                if (awaiter.TrySetResult(packet))
                {
                    return awaiter.Task;
                }
            }

            throw new InvalidOperationException($"Packet of type '{type.Name}' not handled or dispatched.");
        }

        public void Reset()
        {
            _awaiters.Clear();
        }

        public TaskCompletionSource<MqttBasePacket> AddPacketAwaiter<TResponsePacket>(ushort? identifier) where TResponsePacket : MqttBasePacket
        {
            var tcs = new TaskCompletionSource<MqttBasePacket>();

            if (!identifier.HasValue)
            {
                identifier = 0;
            }

            var key = new Tuple<ushort, Type>(identifier ?? 0, typeof(TResponsePacket));
            if (!_awaiters.TryAdd(key, tcs))
            {
                throw new InvalidOperationException($"The packet dispatcher already has an awaiter for packet of type '{key.Item2.Name}' with identifier {key.Item1}.");
            }

            return tcs;
        }

        public void RemovePacketAwaiter<TResponsePacket>(ushort? identifier) where TResponsePacket : MqttBasePacket
        {
            if (!identifier.HasValue)
            {
                identifier = 0;
            }

            var key = new Tuple<ushort, Type>(identifier ?? 0, typeof(TResponsePacket));
            _awaiters.TryRemove(key, out _);
        }
    }
}