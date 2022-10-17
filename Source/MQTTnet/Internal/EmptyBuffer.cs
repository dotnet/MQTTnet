// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace MQTTnet.Internal
{
    public static class EmptyBuffer
    {
        // ReSharper disable once UseArrayEmptyMethod
        public static byte[] Array { get; } = new byte[0];

        // ReSharper disable once UseArrayEmptyMethod
        public static ArraySegment<byte> ArraySegment { get; } = new ArraySegment<byte>(new byte[0], 0, 0);
    }
}