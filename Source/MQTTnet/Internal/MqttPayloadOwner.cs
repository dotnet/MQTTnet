// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Threading.Tasks;

namespace MQTTnet.Internal
{
    public abstract class MqttPayloadOwner : IAsyncDisposable
    {
        private bool _disposed = false;
        public static MqttPayloadOwner Empty { get; } = new EmptyPayloadOwner();

        public abstract ReadOnlySequence<byte> Payload { get; }

        public ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;
                return DisposeAsync(true);
            }
            return ValueTask.CompletedTask;
        }

        protected virtual ValueTask DisposeAsync(bool disposing)
        {
            return ValueTask.CompletedTask;
        }


        private sealed class EmptyPayloadOwner : MqttPayloadOwner
        {
            public override ReadOnlySequence<byte> Payload => ReadOnlySequence<byte>.Empty;
            protected override ValueTask DisposeAsync(bool disposing)
            {
                return ValueTask.CompletedTask;
            }
        }
    }
}
