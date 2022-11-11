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

                Cleanup();
                
                // If there is already a waiting task let it run.
                if (_waiter != null)
                {
                    _waiter.Approve();
                    _waiter = null;

                    // Since we already got a waiter the signal must be reset right now!
                    _isSignaled = false;
                }
            }
        }

        public Task WaitAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lock (_syncRoot)
            {
                ThrowIfDisposed();

                Cleanup();

                if (_isSignaled)
                {
                    _isSignaled = false;
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

        void Cleanup()
        {
            // Cleanup if the previous waiter was cancelled.
            if (_waiter != null && _waiter.Task.IsCanceled)
            {
                _waiter = null;
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