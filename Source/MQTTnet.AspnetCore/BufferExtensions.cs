// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace MQTTnet.AspNetCore;

public static class BufferExtensions
{
    public static ArraySegment<byte> GetArray(this ReadOnlyMemory<byte> memory)
    {
        if (!MemoryMarshal.TryGetArray(memory, out var result))
        {
            throw new InvalidOperationException("Buffer backed by array was expected");
        }

        return result;
    }
}