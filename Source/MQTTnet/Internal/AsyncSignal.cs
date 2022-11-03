// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Internal
{
    public sealed class AsyncSignal : IDisposable
    {
        readonly object _syncRoot = new object();

        bool _isDisposed;
        bool _isSignaled;
        AsyncSignalWaiter _waiter;

        public void Dispose()
        {
            lock (_syncRoot)
            {
                _waiter?.Dispose();
                _waiter = null;

                _isDisposed = true;
            }
        }

        public void Set()
        {
            lock (_syncRoot)
            {
                _isSignaled = true;

                // If there is already a waiting task let it run.
                _waiter?.Approve();
                _waiter = null;
            }
        }

        public Task WaitAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lock (_syncRoot)
            {
                ThrowIfDisposed();

                if (_isSignaled)
                {
                    return CompletedTask.Instance;
                }

                if (_waiter != null)
                {
                    if (!_waiter.Task.IsCompleted)
                    {
                        throw new InvalidOperationException("Only one waiting task is permitted per async signal.");
                    }
                    
                    _waiter.Dispose();
                }

                _waiter = new AsyncSignalWaiter(cancellationToken);
                return _waiter.Task;
            }
        }

        void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(AsyncSignal));
            }
        }

        sealed class AsyncSignalWaiter : IDisposable
        {
            readonly AsyncTaskCompletionSource<bool> _promise = new AsyncTaskCompletionSource<bool>();

            // ReSharper disable once FieldCanBeMadeReadOnly.Local
            CancellationTokenRegistration _cancellationTokenRegistration;

            public AsyncSignalWaiter(CancellationToken cancellationToken)
            {
                if (cancellationToken.CanBeCanceled)
                {
                    _cancellationTokenRegistration = cancellationToken.Register(Cancel);
                }
            }

            public Task Task => _promise.Task;

            public void Approve()
            {
                _promise.TrySetResult(true);
            }

            public void Dispose()
            {
                _cancellationTokenRegistration.Dispose();

                _promise.TrySetException(new ObjectDisposedException(nameof(AsyncSignalWaiter)));
            }

            void Cancel()
            {
                _promise.TrySetCanceled();
            }
        }
    }
}