// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;

namespace MQTTnet.Buffers
{
    /// <summary>
    /// Owner of <see cref="ReadOnlySequence{T}"/> that is responsible
    /// for disposing the underlying payload appropriately.
    /// </summary>
    public struct MqttPayload<T> : IReadOnlySequenceOwner<T>
    {
        public MqttPayload()
        {
            Initialize(ReadOnlySequence<T>.Empty, null);
        }

        public MqttPayload(T[] memory, IDisposable owner = null)
        {
            Initialize(new ReadOnlySequence<T>(memory), owner);
        }

        public MqttPayload(ReadOnlySequence<T> sequence, IDisposable owner = null)
        {
            Initialize(sequence, owner);
        }

        public MqttPayload(ReadOnlyMemory<T> memory, IDisposable owner = null)
        {
            Initialize(new ReadOnlySequence<T>(memory), owner);
        }

        public MqttPayload(ArraySegment<T> memory, IDisposable owner = null)
        {
            Initialize(new ReadOnlySequence<T>(memory), owner);
        }

        private void Initialize(ReadOnlySequence<T> sequence, IDisposable owner)
        {
            _sequence = sequence;
            _owner = owner;
        }

        private ReadOnlySequence<T> _sequence;
        private IDisposable _owner;

        /// <summary>
        /// Gets a <see cref="ReadOnlySequence{T}"/>.
        /// </summary>
        public ReadOnlySequence<T> Sequence { get => _sequence; }

        /// <summary>
        /// Gets the owner of the <see cref="ReadOnlySequence{T}"/>.
        /// </summary>
        public IDisposable Owner => _owner;

        /// <summary>
        /// Frees the underlying memory and sets the <see cref="ReadOnlySequence{T}"/> to empty.
        /// </summary>
        public void Dispose()
        {
            _sequence = ReadOnlySequence<T>.Empty;
            _owner?.Dispose();
        }
    }
}
