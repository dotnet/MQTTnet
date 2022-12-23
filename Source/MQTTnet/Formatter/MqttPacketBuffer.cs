// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using MQTTnet.Implementations;
using MQTTnet.Internal;

namespace MQTTnet.Formatter
{
    public readonly struct MqttPacketBuffer
    {
        static readonly ArraySegment<byte> EmptyPayload = EmptyBuffer.ArraySegment;
        
        public MqttPacketBuffer(ArraySegment<byte> packet, ArraySegment<byte> payload)
        {
            Packet = packet;
            Payload = payload;

            Length = Packet.Count + Payload.Count;
        }
        
        public MqttPacketBuffer(ArraySegment<byte> packet)
        {
            Packet = packet;
            Payload = EmptyPayload;

            Length = Packet.Count;
        }

        public int Length { get; }
        
        public ArraySegment<byte> Packet { get; }
        
        public ArraySegment<byte> Payload { get; }

        public byte[] ToArray()
        {
            if (Payload.Count == 0)
            {
                return Packet.ToArray();
            }

            var buffer = new byte[Length];
            MqttMemoryHelper.Copy(Packet.Array, Packet.Offset, buffer, 0, Packet.Count);
            MqttMemoryHelper.Copy(Payload.Array, Payload.Offset, buffer, Packet.Count, Payload.Count);

            return buffer;
        }
        
        public ArraySegment<byte> Join()
        {
            if (Payload.Count == 0)
            {
                return Packet;
            }

            var buffer = new byte[Length];
            MqttMemoryHelper.Copy(Packet.Array, Packet.Offset, buffer, 0, Packet.Count);
            MqttMemoryHelper.Copy(Payload.Array, Payload.Offset, buffer, Packet.Count, Payload.Count);

            return new ArraySegment<byte>(buffer);
        }
    }
}