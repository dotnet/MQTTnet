// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Buffers;
using System;
using System.Buffers;

namespace MQTTnet.Formatter
{
    public readonly struct MqttPacketBuffer
    {
        public MqttPacketBuffer(ReadOnlySequence<byte> packet, ReadOnlySequence<byte> payload = default)
        {
            Packet = packet;
            Payload = payload;

            if (Packet.Length + Payload.Length > int.MaxValue)
            {
                throw new InvalidOperationException("The packet is too large.");
            }

            Length = (int)Packet.Length + (int)Payload.Length;
        }

        public MqttPacketBuffer(ArraySegment<byte> packet)
        {
            Packet = new ReadOnlySequence<byte>(packet);
            Payload = ReadOnlySequence<byte>.Empty;

            if (Packet.Length > int.MaxValue)
            {
                throw new InvalidOperationException("The packet is too large.");
            }

            Length = (int)Packet.Length;
        }

        public MqttPacketBuffer(ReadOnlySequence<byte> packet) : this(packet, ReadOnlySequence<byte>.Empty)
        {
        }

        public int Length { get; }

        public ReadOnlySequence<byte> Packet { get; }

        public ReadOnlySequence<byte> Payload { get; }

        public byte[] ToArray()
        {
            var buffer = GC.AllocateUninitializedArray<byte>(Length);
            int packetLength = (int)Packet.Length;
            MqttMemoryHelper.Copy(Packet, 0, buffer, 0, packetLength);
            if (Payload.Length > 0)
            {
                int payloadLength = (int)Payload.Length;
                MqttMemoryHelper.Copy(Payload, 0, buffer, packetLength, payloadLength);
            }
            return buffer;
        }

        public IMemoryOwner<byte> ToMemoryOwner()
        {
            var memoryOwner = MemoryPool<byte>.Shared.Rent(Length);
            int packetLength = (int)Packet.Length;
            MqttMemoryHelper.Copy(Packet, 0, memoryOwner.Memory, 0, packetLength);
            if (Payload.Length > 0)
            {
                int payloadLength = (int)Payload.Length;
                MqttMemoryHelper.Copy(Payload, 0, memoryOwner.Memory, packetLength, payloadLength);
            }
            return memoryOwner;
        }

        public ArraySegment<byte> Join()
        {
            return new ArraySegment<byte>(ToArray());
        }
    }
}