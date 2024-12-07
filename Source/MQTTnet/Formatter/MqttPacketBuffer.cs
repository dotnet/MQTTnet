// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;

namespace MQTTnet.Formatter
{
    public readonly struct MqttPacketBuffer
    {
        public int Length { get; }

        public ReadOnlyMemory<byte> Packet { get; }

        public ReadOnlySequence<byte> Payload { get; }

        public MqttPacketBuffer(ReadOnlyMemory<byte> packet)
            : this(packet, ReadOnlySequence<byte>.Empty)
        {
        }

        public MqttPacketBuffer(ReadOnlyMemory<byte> packet, ReadOnlySequence<byte> payload)
        {
            Packet = packet;
            Payload = payload;
            Length = Packet.Length + (int)Payload.Length;
        }

        public byte[] ToArray()
        {
            if (Payload.Length == 0)
            {
                return Packet.ToArray();
            }

            var buffer = GC.AllocateUninitializedArray<byte>(Length);
            Packet.Span.CopyTo(buffer);
            Payload.CopyTo(buffer.AsSpan(Packet.Length));

            return buffer;
        }

        public ReadOnlyMemory<byte> Join()
        {
            if (Payload.Length == 0)
            {
                return Packet;
            }

            var buffer = GC.AllocateUninitializedArray<byte>(Length);
            Packet.Span.CopyTo(buffer);
            Payload.CopyTo(buffer.AsSpan(Packet.Length));

            return buffer;
        }
    }
}