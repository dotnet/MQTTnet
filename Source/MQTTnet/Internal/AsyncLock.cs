// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Internal
{
        public sealed class AsyncLockOldFixed : IDisposable
    {
        readonly IDisposable _releaser;
        readonly object _syncRoot = new object();

        SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        CancellationTokenSource _disposalToken = new CancellationTokenSource();
        
        public AsyncLockOldFixed()
        {
            _releaser = new Releaser(this);
        }

        public void Dispose()
        {
            lock (_syncRoot)
            {
                _disposalToken?.Cancel();
                _disposalToken?.Dispose();
                _disposalToken = null;
                
                _semaphore?.Dispose();
                _semaphore = null;
            }
        }

        public async Task<IDisposable> WaitAsync(CancellationToken cancellationToken)
        {
            Task task;
            
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposalToken.Token);

            // This lock is required to avoid ObjectDisposedExceptions.
            // These are fired when this lock gets disposed (and thus the semaphore)
            // and a worker thread tries to call this method at the same time.
            // Another way would be catching all ObjectDisposedExceptions but this situation happens
            // quite often when clients are disconnecting.
            lock (_syncRoot)
            {
                
                task = _semaphore?.WaitAsync(cts.Token);
            }

            if (task == null)
            {
                throw new ObjectDisposedException("The AsyncLock is disposed.");
            }

            if (task.Status == TaskStatus.RanToCompletion)
            {
                return _releaser;
            }

            // Wait for the _WaitAsync_ method and return the releaser afterwards.
            await task.ConfigureAwait(false);

            return _releaser;
        }

        void Release()
        {
            lock (_syncRoot)
            {
                _semaphore?.Release();
            }
        }

        sealed class Releaser : IDisposable
        {
            readonly AsyncLockOldFixed _lock;

            internal Releaser(AsyncLockOldFixed @lock)
            {
                _lock = @lock;
            }

            public void Dispose()
            {
                _lock.Release();
            }
        }
    }
    
    public sealed class AsyncLockOld : IDisposable
    {
        readonly Task<IDisposable> _releaser;
        readonly object _syncRoot = new object();

        SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public AsyncLockOld()
        {
            _releaser = Task.FromResult((IDisposable)new Releaser(this));
        }

        public void Dispose()
        {
            lock (_syncRoot)
            {
                _semaphore?.Dispose();
                _semaphore = null;
            }
        }

        public Task<IDisposable> WaitAsync(CancellationToken cancellationToken)
        {
            Task task;

            // This lock is required to avoid ObjectDisposedExceptions.
            // These are fired when this lock gets disposed (and thus the semaphore)
            // and a worker thread tries to call this method at the same time.
            // Another way would be catching all ObjectDisposedExceptions but this situation happens
            // quite often when clients are disconnecting.
            lock (_syncRoot)
            {
                task = _semaphore?.WaitAsync(cancellationToken);
            }

            if (task == null)
            {
                throw new ObjectDisposedException("The AsyncLock is disposed.");
            }

            if (task.Status == TaskStatus.RanToCompletion)
            {
                return _releaser;
            }

            // Wait for the _WaitAsync_ method and return the releaser afterwards.
            return task.ContinueWith((_, state) => (IDisposable)state, _releaser.Result, cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        void Release()
        {
            lock (_syncRoot)
            {
                _semaphore?.Release();
            }
        }

        sealed class Releaser : IDisposable
        {
            readonly AsyncLockOld _lock;

            internal Releaser(AsyncLockOld @lock)
            {
                _lock = @lock;
            }

            public void Dispose()
            {
                _lock.Release();
            }
        }
    }

    public sealed class AsyncLock : IDisposable
    {
        /*
         * This async supporting lock does not support reentrancy!
         */

        readonly object _syncRoot = new object();
        volatile bool _isDisposed;

        volatile LinkedList<WaitingTask> _waitingTasks = new LinkedList<WaitingTask>();

        public void Dispose()
        {
            lock (_syncRoot)
            {
                foreach (var waitingTask in _waitingTasks)
                {
                    waitingTask.Fail(new ObjectDisposedException(nameof(AsyncLock)));
                }

                _waitingTasks.Clear();

                _isDisposed = true;
            }
        }

        public async Task<IDisposable> WaitAsync(CancellationToken cancellationToken)
        {
            var waitingTask = new WaitingTask(this);

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(waitingTask.Cancel);
            }

            lock (_syncRoot)
            {
                _waitingTasks.AddLast(waitingTask);

                if (_waitingTasks.Count == 1)
                {
                    // There is no other waiting task apart from the current one.
                    // So we can approve the current task directly.
                    Debug.WriteLine($"AsyncLock: Task {waitingTask.Id} is approved (directly).");
                    waitingTask.Approve();
                }
                else
                {
                    Debug.WriteLine($"AsyncLock: Task {waitingTask.Id} queued.");
                }
            }

            await waitingTask.WaitAsync().ConfigureAwait(false);

            // Now the caller will perform its action and call the releaser after the work is done.

            return waitingTask;
        }

        void Release(WaitingTask releaser)
        {
            lock (_syncRoot)
            {
                if (_isDisposed)
                {
                    // There is no much left to do!
                    return;
                }

                var activeTask = _waitingTasks.First.Value;
                if (!ReferenceEquals(activeTask, releaser))
                {
                    throw new InvalidOperationException("The active task must be the current releaser.");
                }

                _waitingTasks.RemoveFirst();

                Debug.WriteLine($"AsyncLock: Task {activeTask.Id} is done.");

                while (_waitingTasks.Count > 0)
                {
                    var nextTask = _waitingTasks.First.Value;
                    if (!nextTask.IsPending)
                    {
                        // Dequeue all canceled or failed tasks.
                        _waitingTasks.RemoveFirst();
                        continue;
                    }

                    nextTask.Approve();
                    Debug.WriteLine($"AsyncLock: Task {nextTask.Id} is approved.");

                    return;
                }

                Debug.WriteLine("AsyncLock: No Task pending.");
            }
        }

        sealed class WaitingTask : IDisposable
        {
            readonly AsyncLock _asyncLock;
            readonly TaskCompletionSource<object> _promise = new TaskCompletionSource<object>();

            internal WaitingTask(AsyncLock asyncLock)
            {
                _asyncLock = asyncLock;
            }

            public int Id => _promise.Task.Id;

            public bool IsPending => !_promise.Task.IsCanceled && !_promise.Task.IsFaulted && !_promise.Task.IsCompleted;

            public void Approve()
            {
                _promise.TrySetResult(null);
            }

            public void Cancel()
            {
                _promise.TrySetCanceled();
            }

            public void Dispose()
            {
                _asyncLock.Release(this);
            }

            public void Fail(Exception exception)
            {
                _promise.TrySetException(exception);

                Debug.WriteLine($"AsyncLock: Task {Id} failed ({exception.GetType().Name}).");
            }

            public async Task WaitAsync()
            {
                await _promise.Task;
            }
        }
    }
}