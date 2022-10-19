// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MQTTnet.Internal
{
    public static class EmptyBuffer
    {
#if NET452
        public static readonly byte[] Array = new byte[0];
#else
        public static readonly byte[] Array = System.Array.Empty<byte>();
#endif

        public static readonly ArraySegment<byte> ArraySegment = new ArraySegment<byte>(Array, 0, 0);
    }
}