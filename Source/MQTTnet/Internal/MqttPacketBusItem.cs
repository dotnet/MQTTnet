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
        AsyncTaskCompletionSource<bool> _promise;
        Exception _exception;
        bool _isCanceled;
        bool _isCompleted;

        public MqttPacketBusItem(MqttPacket packet)
        {
            Packet = packet ?? throw new ArgumentNullException(nameof(packet));
        }

        public event EventHandler Completed;

        public MqttPacket Packet { get; }

        public void Cancel()
        {
            if (_isCanceled)
            {
                throw new InvalidOperationException("The packet bus item is already canceled.");
            }

            _isCanceled = true;
            _promise?.TrySetCanceled();
        }

        public void Complete()
        {
            if (_isCompleted)
            {
                throw new InvalidOperationException("The packet bus item is already delivered.");
            }

            _isCompleted = true;

            _promise?.TrySetResult(true);
            Completed?.Invoke(this, EventArgs.Empty);
        }

        public void Fail(Exception exception)
        {
            if (_exception != null)
            {
                throw new InvalidOperationException("The packet bus item is already failed.");
            }

            _exception = exception ?? throw new ArgumentNullException(nameof(exception));

            _promise?.TrySetException(exception);
        }

        public Task WaitAsync()
        {
            // The task is being created on demand. The intention is to avoid allocations without any use.
            // This results in the fact that the operation may already be completed / failed / canceled
            // when the task is being created.

            if (_promise == null)
            {
                _promise = new AsyncTaskCompletionSource<bool>();

                if (_exception != null)
                {
                    _promise.TrySetException(_exception);
                }

                if (_isCanceled)
                {
                    _promise.TrySetCanceled();
                }

                if (_isCompleted)
                {
                    _promise.TrySetResult(true);
                }
            }

            return _promise.Task;
        }
    }
}