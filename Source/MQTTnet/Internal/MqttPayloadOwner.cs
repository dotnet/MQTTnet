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
        public static MqttPayloadOwner Empty { get; } = new EmptyPayloadOwner();

        public abstract ReadOnlySequence<byte> Payload { get; }

        public abstract ValueTask DisposeAsync();

        private sealed class EmptyPayloadOwner : MqttPayloadOwner
        {
            public override ReadOnlySequence<byte> Payload => ReadOnlySequence<byte>.Empty;
            public override ValueTask DisposeAsync()
            {
                return default;
            }
        }
    }
}
