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
        //static readonly int SpinCount = (Environment.ProcessorCount == 1 ? 1 : 35) * 4;

        /*
         * This async supporting lock does not support reentrancy!
         */

        readonly List<Releaser> _queuedTasks = new List<Releaser>(64);
        readonly Task<IDisposable> _releaserTaskWithDirectApproval;

        readonly Releaser _releaserWithDirectApproval;

        readonly object _syncRoot = new object();

        bool _isDisposed;
        int _queuedTasksCount;

        public AsyncLock()
        {
            _releaserWithDirectApproval = new Releaser(this, null, CancellationToken.None);
            _releaserTaskWithDirectApproval = Task.FromResult((IDisposable)_releaserWithDirectApproval);
        }

        public void Dispose()
        {
            lock (_syncRoot)
            {
                foreach (var waitingTask in _queuedTasks)
                {
                    waitingTask.Fail(new ObjectDisposedException(nameof(AsyncLock)));
                }

                _queuedTasks.Clear();
                _queuedTasksCount = 0;

                _isDisposed = true;
            }
        }

        public Task<IDisposable> WaitAsync(CancellationToken cancellationToken)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(AsyncLock));
            }

            // Try to wait some time. The lock may gets freed up in a few ms. This should avoid creating
            // lots of tasks where they probably not needed. The behavior is the same as in the _SemaphoreSlim_.
            // if (_queuedTasksCount > 0)
            // {
            //     SpinWait spinner = default;
            //     while (spinner.Count < SpinCount)
            //     {
            //         spinner.SpinOnce();
            //         if (_queuedTasksCount == 0)
            //         {
            //             // The wait was successful. The lock was just released.
            //             break;
            //         }
            //     }
            // }

            var hasDirectApproval = false;
            Releaser releaser;

            lock (_syncRoot)
            {
                if (_queuedTasksCount == 0)
                {
                    // There is no other waiting task apart from the current one.
                    // So we can approve the current task directly.
                    releaser = _releaserWithDirectApproval;
                    hasDirectApproval = true;
                    Debug.WriteLine("AsyncLock: Task directly approved.");
                }
                else
                {
                    releaser = new Releaser(this, new TaskCompletionSource<IDisposable>(), cancellationToken);
                    Debug.WriteLine($"AsyncLock: Task {releaser.Id} queued.");
                }

                _queuedTasks.Add(releaser);
                _queuedTasksCount++;
            }

            if (!hasDirectApproval)
            {
                return releaser.Task;
            }
            
            return _releaserTaskWithDirectApproval;
        }

        void Release(Releaser releaser)
        {
            lock (_syncRoot)
            {
                if (_isDisposed)
                {
                    // There is no much left to do!
                    return;
                }

                var activeTask = _queuedTasks[0];
                if (!ReferenceEquals(activeTask, releaser))
                {
                    throw new InvalidOperationException("The active task must be the current releaser.");
                }

                _queuedTasks.RemoveAt(0);
                _queuedTasksCount--;
                
                Debug.WriteLine($"AsyncLock: Task {activeTask.Id} completed.");

                while (_queuedTasksCount > 0)
                {
                    var nextTask = _queuedTasks[0];
                    if (!nextTask.IsPending)
                    {
                        // Dequeue all canceled or failed tasks.
                        _queuedTasks.RemoveAt(0);
                        _queuedTasksCount--;
                        continue;
                    }

                    nextTask.Approve();
                    Debug.WriteLine($"AsyncLock: Task {nextTask.Id} approved.");

                    return;
                }

                Debug.WriteLine("AsyncLock: No Task pending.");
            }
        }

        sealed class Releaser : IDisposable
        {
            readonly AsyncLock _asyncLock;
            readonly CancellationToken _cancellationToken;
            readonly TaskCompletionSource<IDisposable> _promise;

            CancellationTokenRegistration _cancellationTokenRegistration;

            internal Releaser(AsyncLock asyncLock, TaskCompletionSource<IDisposable> promise, CancellationToken cancellationToken)
            {
                _asyncLock = asyncLock ?? throw new ArgumentNullException(nameof(asyncLock));
                _promise = promise;
                _cancellationToken = cancellationToken;

                if (cancellationToken.CanBeCanceled)
                {
                    _cancellationTokenRegistration = cancellationToken.Register(Cancel);
                }
            }

            public int Id => _promise?.Task?.Id ?? -1;

            public bool IsPending => _promise != null && !(_promise.Task.IsCanceled && !_promise.Task.IsFaulted && !_promise.Task.IsCompleted);

            public Task<IDisposable> Task => _promise?.Task;

            public void Approve()
            {
                _promise?.TrySetResult(this);
            }

            public void Dispose()
            {
                if (_cancellationToken.CanBeCanceled)
                {
                    _cancellationTokenRegistration.Dispose();
                }

                _asyncLock?.Release(this);
            }

            public void Fail(Exception exception)
            {
                _promise?.TrySetException(exception);

                Debug.WriteLine($"AsyncLock: Task {Id} failed ({exception.GetType().Name}).");
            }

            void Cancel()
            {
                _promise?.TrySetCanceled();
            }
        }
    }
}