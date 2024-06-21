// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;

namespace MQTTnet.Adapter;

public readonly struct ReceivedMqttPacket : IDisposable
{
    private readonly bool _arrayPool;
    public static readonly ReceivedMqttPacket Empty = new();

    public ReceivedMqttPacket(byte fixedHeader, ArraySegment<byte> body, int totalLength)
    {
        FixedHeader = fixedHeader;
        Body = body;
        TotalLength = totalLength;
        _arrayPool = false;
    }

    public ReceivedMqttPacket(byte fixedHeader, int bodyLength, int totalLength)
    {
        FixedHeader = fixedHeader;
        Body = new ArraySegment<byte>(ArrayPool<byte>.Shared.Rent(bodyLength), 0, bodyLength);
        TotalLength = totalLength;
        _arrayPool = true;
    }

    public void Dispose()
    {
        if (_arrayPool)
        {
            ArrayPool<byte>.Shared.Return(Body.Array);
        }
    }

    public byte FixedHeader { get; }

    public ArraySegment<byte> Body { get; }

    public int TotalLength { get; }
}