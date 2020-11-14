using MQTTnet.Exceptions;
using MQTTnet.Packets;
using System;
using System.Collections.Concurrent;

namespace MQTTnet.PacketDispatcher
{
    public sealed class MqttPacketDispatcher
    {
        readonly ConcurrentDictionary<Tuple<ushort, Type>, IMqttPacketAwaiter> _awaiters = new ConcurrentDictionary<Tuple<ushort, Type>, IMqttPacketAwaiter>();

        public void Dispatch(Exception exception)
        {
            foreach (var awaiter in _awaiters)
            {
                awaiter.Value.Fail(exception);
            }

            _awaiters.Clear();
        }

        public void Dispatch(MqttBasePacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

            if (packet is MqttDisconnectPacket disconnectPacket)
            {
                foreach (var packetAwaiter in _awaiters)
                {
                    packetAwaiter.Value.Fail(new MqttUnexpectedDisconnectReceivedException(disconnectPacket));
                }

                return;
            }

            ushort identifier = 0;
            if (packet is IMqttPacketWithIdentifier packetWithIdentifier && packetWithIdentifier.PacketIdentifier > 0)
            {
                identifier = packetWithIdentifier.PacketIdentifier;
            }

            var type = packet.GetType();
            var key = new Tuple<ushort, Type>(identifier, type);

            if (_awaiters.TryRemove(key, out var awaiter))
            {
                awaiter.Complete(packet);
                return;
            }

            throw new MqttProtocolViolationException($"Received packet '{packet}' at an unexpected time.");
        }

        public void Cancel()
        {
            foreach (var awaiter in _awaiters)
            {
                awaiter.Value.Cancel();
            }

            _awaiters.Clear();
        }

        public MqttPacketAwaiter<TResponsePacket> AddAwaiter<TResponsePacket>(ushort? identifier) where TResponsePacket : MqttBasePacket
        {
            if (!identifier.HasValue)
            {
                identifier = 0;
            }

            var awaiter = new MqttPacketAwaiter<TResponsePacket>(identifier, this);

            var key = new Tuple<ushort, Type>(identifier.Value, typeof(TResponsePacket));
            if (!_awaiters.TryAdd(key, awaiter))
            {
                throw new InvalidOperationException($"The packet dispatcher already has an awaiter for packet of type '{key.Item2.Name}' with identifier {key.Item1}.");
            }

            return awaiter;
        }

        public void RemoveAwaiter<TResponsePacket>(ushort? identifier) where TResponsePacket : MqttBasePacket
        {
            if (!identifier.HasValue)
            {
                identifier = 0;
            }

            var key = new Tuple<ushort, Type>(identifier.Value, typeof(TResponsePacket));
            _awaiters.TryRemove(key, out _);
        }
    }
}