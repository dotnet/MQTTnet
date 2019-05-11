using System;
using System.Collections.Concurrent;
using MQTTnet.Exceptions;
using MQTTnet.Packets;

namespace MQTTnet.PacketDispatcher
{
    public class MqttPacketDispatcher
    {
        private readonly ConcurrentDictionary<Tuple<ushort, Type>, IMqttPacketAwaiter> _packetAwaiters = new ConcurrentDictionary<Tuple<ushort, Type>, IMqttPacketAwaiter>();

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

            if (packet is MqttDisconnectPacket disconnectPacket)
            {
                foreach (var packetAwaiter in _packetAwaiters)
                {
                    packetAwaiter.Value.Fail(new MqttUnexpectedDisconnectReceivedException(disconnectPacket));
                }

                return;
            }

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

            throw new MqttProtocolViolationException($"Received packet '{packet}' at an unexpected time.");
        }

        public void Reset()
        {
            foreach (var awaiter in _packetAwaiters)
            {
                 awaiter.Value.Cancel();
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