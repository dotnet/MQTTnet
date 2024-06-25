// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;

namespace MQTTnet.Buffers
{
    /// <summary>
    /// Owner of <see cref="ReadOnlySequence{T}"/> that is responsible
    /// for disposing the underlying memory appropriately.
    /// </summary>
    public interface IReadOnlySequenceOwner<T> : IDisposable
    {
        /// <summary>
        /// Gets a <see cref="ReadOnlySequence{T}"/>.
        /// </summary>
        ReadOnlySequence<T> Sequence { get; }
    }
}
