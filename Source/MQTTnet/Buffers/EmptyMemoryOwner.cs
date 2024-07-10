// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;

namespace MQTTnet.Buffers
{
    /// <summary>
    /// Holds an empty array of type <typeparamref name="T"/> and
    /// provides a <see cref="Memory{T}"/> view of it.
    /// The static memory is not disposed.
    /// </summary>
    public struct EmptyMemoryOwner<T> : IMemoryOwner<T>
    {
        public static IMemoryOwner<T> Empty { get; } = new EmptyMemoryOwner<T>();

        private T[] _array;

        public EmptyMemoryOwner()
        {
            _array = Array.Empty<T>();
        }

        /// <inheritdoc/>
        public Memory<T> Memory => _array.AsMemory();

        /// <summary>
        /// Nothing to do.
        /// </summary>
        public void Dispose()
        {
        }
    }
}
