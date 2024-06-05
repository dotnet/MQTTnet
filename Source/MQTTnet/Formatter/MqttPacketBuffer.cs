// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Internal;
using System;
using System.Buffers;

namespace MQTTnet.Formatter
{
    public readonly struct MqttPacketBuffer
    {
        static readonly ReadOnlySequence<byte> EmptySequence = EmptyBuffer.ArraySequence;

        public MqttPacketBuffer(ArraySegment<byte> packet, ReadOnlySequence<byte> payload)
        {
            Packet = packet;
            Payload = payload;

            if (Packet.Count + (int)Payload.Length > int.MaxValue)
            {
                throw new InvalidOperationException("The packet is too large.");
            }

            Length = Packet.Count + (int)Payload.Length;
        }

        public MqttPacketBuffer(ArraySegment<byte> packet)
        {
            Packet = packet;
            Payload = EmptySequence;

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

            var buffer = GC.AllocateUninitializedArray<byte>(Length);

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

            var buffer = GC.AllocateUninitializedArray<byte>(Length);

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