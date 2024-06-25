// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;

namespace MQTTnet.Buffers
{
    /// <summary>
    /// Owner of memory rented from <see cref="ArrayPool{T}.Shared"/> that 
    /// is responsible for disposing the underlying memory appropriately.
    /// </summary>
    public sealed class ArrayPoolMemoryOwner<T> : IMemoryOwner<T>
    {
        public static ArrayPoolMemoryOwner<T> Rent(int length)
        {
            var memory = ArrayPool<T>.Shared.Rent(length);
            return new ArrayPoolMemoryOwner<T>(memory);
        }

        private ArrayPoolMemoryOwner(T[] memory)
        {
            Initialize(memory);
        }

        private void Initialize(T[] array)
        {
            _array = array;
        }

        private T[] _array;

        /// <summary>
        /// Gets the rented memory./>.
        /// </summary>
        public T[] Array => _array;

        /// <inheritdoc/>
        public Memory<T> Memory => _array.AsMemory();

        /// <summary>
        /// Returns the underlying memory and sets the <see cref="Array"/> to null.
        /// </summary>
        public void Dispose()
        {
            if (_array != null)
            {
                ArrayPool<T>.Shared.Return(_array);
                _array = null;
            }
        }
    }
}
