// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using MQTTnet.Packets;

namespace MQTTnet.Internal
{
    public sealed class MqttPacketBusItem
    {
        readonly CrossPlatformPromise<int> _promise = new CrossPlatformPromise<int>();

        public MqttPacketBusItem(MqttPacket packet)
        {
            Packet = packet ?? throw new ArgumentNullException(nameof(packet));
        }
        
        public MqttPacket Packet { get; }

        public event EventHandler Delivered;

        public Task WaitForDeliveryAsync()
        {
            return _promise.Task;
        }
        
        public void MarkAsDelivered()
        {
            if (_promise.TrySetResult(0))
            {
                Delivered?.Invoke(this, EventArgs.Empty);
            }
        }

        public void MarkAsFailed(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));
            
            _promise.TrySetException(exception);
        }

        public void MarkAsCancelled()
        {
            _promise.TrySetCanceled();
        }
    }
}