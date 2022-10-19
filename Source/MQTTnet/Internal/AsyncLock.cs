// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Internal
{
    public sealed class AsyncLock : IDisposable
    {
        readonly Task<IDisposable> _completedTask;
        readonly IDisposable _releaser;
        readonly object _syncRoot = new object();

        readonly Queue<AsyncLockWaiter> _waiters = new Queue<AsyncLockWaiter>();
        bool _isDisposed;

        bool _isLocked;

        public AsyncLock()
        {
            _releaser = new Releaser(this);
            _completedTask = Task.FromResult(_releaser);
        }

        public void Dispose()
        {
            lock (_syncRoot)
            {
                _isDisposed = true;

                while (_waiters.Any())
                {
                    _waiters.Dequeue().Dispose();
                }
            }
        }

        public Task<IDisposable> EnterAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(AsyncLock));
            }

            lock (_syncRoot)
            {
                if (!_isLocked)
                {
                    _isLocked = true;
                    return _completedTask;
                }

                var waiter = new AsyncLockWaiter(cancellationToken);
                _waiters.Enqueue(waiter);

                return waiter.Task;
            }
        }

        void Release()
        {
            lock (_syncRoot)
            {
                if (_isDisposed)
                {
                    // All waiters have been canceled with a ObjectDisposedException.
                    // So there is nothing left to do.
                    return;
                }

                // Assume that there is no waiter left first.
                _isLocked = false;

                // Try to find the next waiter which can be approved.
                // Some of them might be canceled already so it is not
                // guaranteed that the very next waiter is the correct one.
                while (_waiters.Any())
                {
                    var waiter = _waiters.Dequeue();
                    var isApproved = waiter.Approve(_releaser);
                    waiter.Dispose();
				
                    if (isApproved)
                    {
                        _isLocked = true;
                        return;
                    }
                }
            }
        }

        sealed class AsyncLockWaiter : IDisposable
        {
            readonly CancellationTokenRegistration _cancellationRegistration;
            readonly bool _hasCancellationRegistration;
            readonly AsyncTaskCompletionSource<IDisposable> _promise = new AsyncTaskCompletionSource<IDisposable>();

            public AsyncLockWaiter(CancellationToken cancellationToken)
            {
                if (cancellationToken.CanBeCanceled)
                {
                    _cancellationRegistration = cancellationToken.Register(Cancel);
                    _hasCancellationRegistration = true;
                }
            }

            public Task<IDisposable> Task => _promise.Task;

            public bool Approve(IDisposable scope)
            {
                if (scope == null)
                {
                    throw new ArgumentNullException(nameof(scope));
                }

                if (_promise.Task.IsCompleted)
                {
                    return false;
                }
                
                return _promise.TrySetResult(scope);
            }

            public void Dispose()
            {
                if (_hasCancellationRegistration)
                {
                    _cancellationRegistration.Dispose();
                }

                _promise.TrySetException(new ObjectDisposedException(nameof(AsyncLockWaiter)));
            }

            void Cancel()
            {
                _promise.TrySetCanceled();
            }
        }

        readonly struct Releaser : IDisposable
        {
            readonly AsyncLock _asyncLock;

            public Releaser(AsyncLock asyncLock)
            {
                _asyncLock = asyncLock ?? throw new ArgumentNullException(nameof(asyncLock));
            }

            public void Dispose()
            {
                _asyncLock.Release();
            }
        }
    }
}