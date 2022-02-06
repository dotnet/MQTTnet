// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Packets;
using System;
using System.Collections.Generic;

namespace MQTTnet.PacketDispatcher
{
    public sealed class MqttPacketDispatcher
    {
        readonly List<IMqttPacketAwaitable> _awaitables = new List<IMqttPacketAwaitable>();

        public void FailAll(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));

            lock (_awaitables)
            {
                foreach (var awaitable in _awaitables)
                {
                    awaitable.Fail(exception);
                }

                _awaitables.Clear();
            }
        }

        public void CancelAll()
        {
            lock (_awaitables)
            {
                foreach (var awaitable in _awaitables)
                {
                    awaitable.Cancel();
                }

                _awaitables.Clear();
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
            var awaitables = new List<IMqttPacketAwaitable>();
            
            lock (_awaitables)
            {
                for (var i = _awaitables.Count - 1; i >= 0; i--)
                {
                    var entry = _awaitables[i];

                    // Note: The PingRespPacket will also arrive here and has NO identifier but there
                    // is code which waits for it. So the code must be able to deal with filters which
                    // are referring to the type only (identifier is 0)!
                    if (entry.Filter.Type != packetType || entry.Filter.Identifier != identifier)
                    {
                        continue;
                    }
                    
                    awaitables.Add(entry);
                    _awaitables.RemoveAt(i);
                }
            }
            
            foreach (var matchingEntry in awaitables)
            {
                matchingEntry.Complete(packet);
            }

            return awaitables.Count > 0;
        }
        
        public MqttPacketAwaitable<TResponsePacket> AddAwaitable<TResponsePacket>(ushort packetIdentifier) where TResponsePacket : MqttBasePacket
        {
            var awaitable = new MqttPacketAwaitable<TResponsePacket>(packetIdentifier, this);

            lock (_awaitables)
            {
                _awaitables.Add(awaitable);
            }
            
            return awaitable;
        }

        public void RemoveAwaitable(IMqttPacketAwaitable awaitable)
        {
            if (awaitable == null) throw new ArgumentNullException(nameof(awaitable));
            
            lock (_awaitables)
            {
                _awaitables.Remove(awaitable);
            }
        }
    }
}