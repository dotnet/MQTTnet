// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Buffers;

namespace MQTTnet.Buffers
{
    /// <summary>
    /// Helper to build a ReadOnlySequence from a set of <see cref="ArrayPool{T}"/> allocated buffers.
    /// </summary>
    public sealed class ArrayPoolBufferSegment<T> : ReadOnlySequenceSegment<T>
    {
        private T[] _array;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrayPoolBufferSegment{T}"/> class.
        /// </summary>
        public ArrayPoolBufferSegment(T[] array, int offset, int length)
        {
            Memory = new ReadOnlyMemory<T>(array, offset, length);
            _array = array;
        }

        /// <summary>
        /// Returns the base array of the buffer.
        /// </summary>
        public T[] Array() => _array;

        /// <summary>
        /// Rents a buffer from the pool and returns a <see cref="ArrayPoolBufferSegment{T}"/> instance.
        /// </summary>
        /// <param name="length">The length of the segment.</param>
        public static ArrayPoolBufferSegment<T> Rent(int length)
        {
            var array = ArrayPool<T>.Shared.Rent(length);
            return new ArrayPoolBufferSegment<T>(array, 0, length);
        }

        /// <summary>
        /// Rents a new buffer and appends it to the sequence.
        /// </summary>
        public ArrayPoolBufferSegment<T> RentAndAppend(int length)
        {
            var array = ArrayPool<T>.Shared.Rent(length);
            return Append(array, 0, length);
        }

        /// <summary>
        /// Appends a buffer to the sequence.
        /// </summary>
        public ArrayPoolBufferSegment<T> Append(T[] array, int offset, int length)
        {
            var segment = new ArrayPoolBufferSegment<T>(array, offset, length)
            {
                RunningIndex = RunningIndex + Memory.Length,
            };
            Next = segment;
            return segment;
        }
    }
}
