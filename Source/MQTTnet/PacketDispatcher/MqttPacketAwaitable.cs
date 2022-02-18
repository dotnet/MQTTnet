// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MQTTnet.Exceptions;
using MQTTnet.Packets;
using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Internal;

namespace MQTTnet.PacketDispatcher
{
    public sealed class MqttPacketAwaitable<TPacket> : IMqttPacketAwaitable where TPacket : MqttPacket
    {
        readonly AsyncTaskCompletionSource<MqttPacket> _promise = new AsyncTaskCompletionSource<MqttPacket>();
        readonly MqttPacketDispatcher _owningPacketDispatcher;

        public MqttPacketAwaitable(ushort packetIdentifier, MqttPacketDispatcher owningPacketDispatcher)
        {
            Filter = new MqttPacketAwaitableFilter
            {
                Type = typeof(TPacket),
                Identifier = packetIdentifier
            };
            
            _owningPacketDispatcher = owningPacketDispatcher ?? throw new ArgumentNullException(nameof(owningPacketDispatcher));
        }
        
        public MqttPacketAwaitableFilter Filter { get; }
        
        public async Task<TPacket> WaitOneAsync(CancellationToken cancellationToken)
        {
            using (cancellationToken.Register(() => Fail(new MqttCommunicationTimedOutException())))
            {
                var packet = await _promise.Task.ConfigureAwait(false);
                return (TPacket)packet;
            }
        }

        public void Complete(MqttPacket packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));

            _promise.TrySetResult(packet);
        }

        public void Fail(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));
            
            _promise.TrySetException(exception);
        }

        public void Cancel()
        {
            _promise.TrySetCanceled();
        }

        public void Dispose()
        {
            _owningPacketDispatcher.RemoveAwaitable(this);
        }
    }
}