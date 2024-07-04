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
    public class MqttPayloadOwner<T> : IReadOnlySequenceOwner<T>
    {
        public MqttPayloadOwner()
        {
            Initialize(ReadOnlySequence<T>.Empty, null);
        }

        public MqttPayloadOwner(T[] memory, IDisposable owner = null)
        {
            Initialize(new ReadOnlySequence<T>(memory), owner);
        }

        public MqttPayloadOwner(ReadOnlySequence<T> sequence, IDisposable owner = null)
        {
            Initialize(sequence, owner);
        }

        public MqttPayloadOwner(ReadOnlyMemory<T> memory, IDisposable owner = null)
        {
            Initialize(new ReadOnlySequence<T>(memory), owner);
        }

        public MqttPayloadOwner(ArraySegment<T> memory, IDisposable owner = null)
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
        /// Gets the length of the <see cref="ReadOnlySequence{T}"/>.
        /// </summary>
        public long Length => _sequence.Length;

        /// <summary>
        /// Frees the underlying memory and sets the <see cref="ReadOnlySequence{T}"/> to empty.
        /// </summary>
        public void Dispose()
        {
            _sequence = ReadOnlySequence<T>.Empty;
            _owner?.Dispose();
            _owner = null;
        }

        /// <summary>
        /// Returns a new <see cref="MqttPayloadOwner{T}"/> with the same
        /// <see cref="ReadOnlySequence{T}"/> and transfers the ownership
        /// to the caller.
        /// </summary>
        public MqttPayloadOwner<T> TransferOwnership()
        {
            var payload = new MqttPayloadOwner<T>(_sequence, _owner);
            _owner = null;
            return payload;
        }

        public static implicit operator MqttPayloadOwner<T>(ArrayPoolMemoryOwner<T> memoryOwner) => new MqttPayloadOwner<T>(memoryOwner.Memory, memoryOwner);
        public static implicit operator MqttPayloadOwner<T>(ReadOnlySequence<T> sequence) => new MqttPayloadOwner<T>(sequence);
        public static implicit operator MqttPayloadOwner<T>(ReadOnlyMemory<T> memory) => new MqttPayloadOwner<T>(memory);
        public static implicit operator MqttPayloadOwner<T>(ArraySegment<T> memory) => new MqttPayloadOwner<T>(memory);
        public static implicit operator MqttPayloadOwner<T>(T[] memory) => new MqttPayloadOwner<T>(memory);
    }
}
