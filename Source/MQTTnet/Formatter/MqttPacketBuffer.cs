// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Linq;
using MQTTnet.Implementations;
using MQTTnet.Internal;

namespace MQTTnet.Formatter
{
    public readonly struct MqttPacketBuffer
    {
        static readonly ArraySegment<byte> EmptyPayload = EmptyBuffer.ArraySegment;

        public MqttPacketBuffer(ArraySegment<byte> packet, ReadOnlySequence<byte> payload)
        {
            Packet = packet;
            Payload = payload;

            Length = Packet.Count + (int)Payload.Length;
        }

        public MqttPacketBuffer(ArraySegment<byte> packet)
        {
            Packet = packet;
            Payload = EmptyBuffer.ArraySequence;

            Length = Packet.Count;
        }

        public int Length { get; }

        public ArraySegment<byte> Packet { get; }

        public ReadOnlySequence<byte> Payload { get; }

        public byte[] ToArray()
        {
            if (Payload.Length == 0)
            {
                return Packet.ToArray();
            }

            var buffer = new byte[Length];
            MqttMemoryHelper.Copy(Packet.Array, Packet.Offset, buffer, 0, Packet.Count);

            int offset = Packet.Count;
            foreach (ReadOnlyMemory<byte> segment in Payload)
            {
                segment.CopyTo(buffer.AsMemory(offset));
                offset += segment.Length;
            }

            return buffer;
        }

        public ArraySegment<byte> Join()
        {
            if (Payload.Length == 0)
            {
                return Packet;
            }

            var buffer = new byte[Length];
            MqttMemoryHelper.Copy(Packet.Array, Packet.Offset, buffer, 0, Packet.Count);
            int offset = Packet.Count;
            foreach (ReadOnlyMemory<byte> segment in Payload)
            {
                segment.CopyTo(buffer.AsMemory(offset));
                offset += segment.Length;
            }

            return new ArraySegment<byte>(buffer);
        }
    }
}