// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;

namespace MQTTnet.Internal
{
    public static class EmptyBuffer
    {
        public static readonly byte[] Array = System.Array.Empty<byte>();

        public static readonly ArraySegment<byte> ArraySegment = new ArraySegment<byte>(Array, 0, 0);
        public static readonly ReadOnlySequence<byte> ArraySequence = new ReadOnlySequence<byte>();
    }
}