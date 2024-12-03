// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;

namespace MQTTnet.Adapter;

public readonly struct ReceivedMqttPacket
{
    public static readonly ReceivedMqttPacket Empty = new();

    public ReceivedMqttPacket(byte fixedHeader, ReadOnlySequence<byte> body, int totalLength)
    {
        FixedHeader = fixedHeader;
        Body = body;
        TotalLength = totalLength;
    }

    public byte FixedHeader { get; }

    public ReadOnlySequence<byte> Body { get; }

    public int TotalLength { get; }
}