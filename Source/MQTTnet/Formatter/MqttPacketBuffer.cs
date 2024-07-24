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
        public MqttPacketBuffer(ArraySegment<byte> packet, ReadOnlySequence<byte> payload)
        {
            Packet = packet;
            Payload = payload;

            Length = Packet.Count + (int)Payload.Length;
        }

        public MqttPacketBuffer(ArraySegment<byte> packet)
        {
            Packet = packet;
            Payload = EmptyBuffer.ReadOnlySequence;

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
            MqttMemoryHelper.Copy(Payload, 0, buffer, Packet.Count, (int)Payload.Length);

            return buffer;
        }

        public ArraySegment<byte> Join()
        {
            if (Payload.Length == 0)
            {
                return Packet;
            }
            return new ArraySegment<byte>(this.ToArray());
        }
    }
}