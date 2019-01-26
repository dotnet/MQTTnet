using System;
using System.Collections.Concurrent;
using System.Threading;
using MQTTnet.Packets;

namespace MQTTnet.PacketDispatcher
{
    public class MqttPacketDispatcher
    {
        private readonly ConcurrentDictionary<Tuple<ushort, Type>, IMqttPacketAwaiter> _packetAwaiters = new ConcurrentDictionary<Tuple<ushort, Type>, IMqttPacketAwaiter>();

        private BlockingCollection<MqttBasePacket> _inboundPackagesQueue = new BlockingCollection<MqttBasePacket>();

        public void Dispatch(Exception exception)
        {
            foreach (var awaiter in _packetAwaiters)
            {
                awaiter.Value.Fail(exception);
            }

            _packetAwaiters.Clear();
        }

        public void Dispatch(MqttBasePacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

            ushort identifier = 0;
            if (packet is IMqttPacketWithIdentifier packetWithIdentifier && packetWithIdentifier.PacketIdentifier.HasValue)
            {
                identifier = packetWithIdentifier.PacketIdentifier.Value;
            }

            var type = packet.GetType();
            var key = new Tuple<ushort, Type>(identifier, type);

            if (_packetAwaiters.TryRemove(key, out var awaiter))
            {
                awaiter.Complete(packet);
                return;
            }

            lock (_inboundPackagesQueue)
            {
                _inboundPackagesQueue.Add(packet);
            }
        }

        public MqttBasePacket Take(CancellationToken cancellationToken)
        {
            BlockingCollection<MqttBasePacket> inboundPackagesQueue;
            lock (_inboundPackagesQueue)
            {
                inboundPackagesQueue = _inboundPackagesQueue;
            }

            return inboundPackagesQueue.Take(cancellationToken);
        }

        public void Reset()
        {
            foreach (var awaiter in _packetAwaiters)
            {
                awaiter.Value.Cancel();
            }

            lock (_inboundPackagesQueue)
            {
                _inboundPackagesQueue?.Dispose();
                _inboundPackagesQueue = new BlockingCollection<MqttBasePacket>();
            }

            _packetAwaiters.Clear();
        }

        public MqttPacketAwaiter<TResponsePacket> AddPacketAwaiter<TResponsePacket>(ushort? identifier) where TResponsePacket : MqttBasePacket
        {
            if (!identifier.HasValue)
            {
                identifier = 0;
            }

            var awaiter = new MqttPacketAwaiter<TResponsePacket>(identifier, this);

            var key = new Tuple<ushort, Type>(identifier.Value, typeof(TResponsePacket));
            if (!_packetAwaiters.TryAdd(key, awaiter))
            {
                throw new InvalidOperationException($"The packet dispatcher already has an awaiter for packet of type '{key.Item2.Name}' with identifier {key.Item1}.");
            }

            return awaiter;
        }

        public void RemovePacketAwaiter<TResponsePacket>(ushort? identifier) where TResponsePacket : MqttBasePacket
        {
            if (!identifier.HasValue)
            {
                identifier = 0;
            }

            var key = new Tuple<ushort, Type>(identifier.Value, typeof(TResponsePacket));
            _packetAwaiters.TryRemove(key, out _);
        }
    }
}