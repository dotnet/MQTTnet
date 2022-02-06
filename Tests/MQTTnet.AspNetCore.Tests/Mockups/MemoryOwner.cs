// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;

namespace MQTTnet.AspNetCore.Tests.Mockups
{
    public class MemoryOwner : IMemoryOwner<byte>
    {
        private readonly byte[] _raw;

        public MemoryOwner(int size)
        {
            if (size <= 0)
            {
                size = 1024;
            }

            if (size > 4096)
            {
                size = 4096;
            }

            _raw = ArrayPool<byte>.Shared.Rent(size);
            Memory = _raw;
        }

        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(_raw);
        }

        public Memory<byte> Memory { get; }
    }
}