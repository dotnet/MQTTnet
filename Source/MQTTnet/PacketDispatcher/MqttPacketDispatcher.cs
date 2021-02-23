using MQTTnet.Packets;
using System;
using System.Collections.Generic;

namespace MQTTnet.PacketDispatcher
{
    public sealed class MqttPacketDispatcher
    {
        readonly List<IMqttPacketAwaiter> _awaiters = new List<IMqttPacketAwaiter>();

        public void FailAll(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));

            lock (_awaiters)
            {
                foreach (var awaiter in _awaiters)
                {
                    awaiter.Fail(exception);
                }

                _awaiters.Clear();
            }
        }

        public void CancelAll()
        {
            lock (_awaiters)
            {
                foreach (var entry in _awaiters)
                {
                    entry.Cancel();
                }

                _awaiters.Clear();
            }
        }
        
        public bool TryDispatch(MqttBasePacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            
            ushort identifier = 0;
            if (packet is IMqttPacketWithIdentifier packetWithIdentifier)
            {
                identifier = packetWithIdentifier.PacketIdentifier;
            }

            var packetType = packet.GetType();
            var matchingAwaiters = new List<IMqttPacketAwaiter>();
            
            lock (_awaiters)
            {
                for (var i = _awaiters.Count - 1; i >= 0; i--)
                {
                    var entry = _awaiters[i];

                    // Note: The PingRespPacket will also arrive here and has NO identifier but there
                    // is code which waits for it. So the code must be able to deal with filters which
                    // are referring to the type only (identifier is 0)!
                    if (entry.PacketFilter.Type != packetType || entry.PacketFilter.Identifier != identifier)
                    {
                        continue;
                    }
                    
                    matchingAwaiters.Add(entry);
                    _awaiters.RemoveAt(i);
                }
            }
            
            foreach (var matchingEntry in matchingAwaiters)
            {
                matchingEntry.Complete(packet);
            }

            return matchingAwaiters.Count > 0;
        }
        
        public MqttPacketAwaiter<TResponsePacket> AddAwaiter<TResponsePacket>(ushort packetIdentifier) where TResponsePacket : MqttBasePacket
        {
            var awaiter = new MqttPacketAwaiter<TResponsePacket>(packetIdentifier, this);

            lock (_awaiters)
            {
                _awaiters.Add(awaiter);
            }
            
            return awaiter;
        }

        public void RemoveAwaiter(IMqttPacketAwaiter awaiter)
        {
            if (awaiter == null) throw new ArgumentNullException(nameof(awaiter));
            
            lock (_awaiters)
            {
                _awaiters.Remove(awaiter);
            }
        }
    }
}