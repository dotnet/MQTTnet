// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Implementations;

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
        /*
         * This async supporting lock does not support reentrancy!
         */

        readonly object _syncRoot = new object();
        
        bool _isDisposed;
        readonly LinkedList<QueuedTask> _queuedTasks = new LinkedList<QueuedTask>();
        int _queuedTasksCount;

        readonly QueuedTask _queuedTaskWithDirectApproval;

        public AsyncLock()
        {
            _queuedTaskWithDirectApproval = new QueuedTask(this);
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

        public async Task<IDisposable> WaitAsync(CancellationToken cancellationToken)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(AsyncLock));
            }
            
            var hasDirectApproval = false;
            QueuedTask queuedTask;
            
            lock (_syncRoot)
            {
                _queuedTasksCount++;
                
                if (_queuedTasks.Count == 0)
                {
                    // There is no other waiting task apart from the current one.
                    // So we can approve the current task directly.
                    //Debug.WriteLine("AsyncLock: Task directly approved.");
                    hasDirectApproval = true;
                    
                    queuedTask = _queuedTaskWithDirectApproval;
                }
                else
                {
                    queuedTask = new QueuedTask(this);
                    
                    if (cancellationToken.CanBeCanceled)
                    {
                        //cancellationToken.Register(queuedTask.Cancel);
                    }
                    
                    //Debug.WriteLine($"AsyncLock: Task {queuedTask.Id} queued.");
                    queuedTask.Queue();
                }

                _queuedTasks.AddLast(queuedTask);
            }

            if (!hasDirectApproval)
            {
                await queuedTask.WaitAsync().ConfigureAwait(false);
            }

            // Now the caller will perform its action and call the releaser after the work is done.

            return queuedTask;
        }

        void Release(QueuedTask releaser)
        {
            lock (_syncRoot)
            {
                if (_isDisposed)
                {
                    // There is no much left to do!
                    return;
                }

                _queuedTasksCount--;
                
                var activeTask = _queuedTasks.First.Value;
                if (!ReferenceEquals(activeTask, releaser))
                {
                    throw new InvalidOperationException("The active task must be the current releaser.");
                }

                _queuedTasks.RemoveFirst();

                //Debug.WriteLine($"AsyncLock: Task {activeTask.Id} done.");

                while (_queuedTasks.Count > 0)
                {
                    var nextTask = _queuedTasks.First.Value;
                    if (!nextTask.IsPending)
                    {
                        // Dequeue all canceled or failed tasks.
                        _queuedTasks.RemoveFirst();
                        continue;
                    }

                    nextTask.Approve();
                    //Debug.WriteLine($"AsyncLock: Task {nextTask.Id} approved.");

                    return;
                }

                //Debug.WriteLine("AsyncLock: No Task pending.");
            }
        }

        sealed class QueuedTask : IDisposable
        {
            readonly AsyncLock _asyncLock;

            TaskCompletionSource<object> _promise;
            
            internal QueuedTask(AsyncLock asyncLock)
            {
                _asyncLock = asyncLock ?? throw new ArgumentNullException(nameof(asyncLock));
            }

            public int Id => _promise?.Task.Id ?? -1;

            public bool IsPending => !_promise.Task.IsCanceled && !_promise.Task.IsFaulted && !_promise.Task.IsCompleted;

            public void Approve()
            {
                _promise?.TrySetResult(null);
            }

            public void Cancel()
            {
                _promise?.TrySetCanceled();
            }

            public void Dispose()
            {
                _asyncLock?.Release(this);
            }

            public void Queue()
            {
                _promise = new TaskCompletionSource<object>();
            }
            
            public void Fail(Exception exception)
            {
                _promise?.TrySetException(exception);

                //Debug.WriteLine($"AsyncLock: Task {Id} failed ({exception.GetType().Name}).");
            }

            public Task WaitAsync()
            {
                return _promise?.Task ?? PlatformAbstractionLayer.CompletedTask;
            }
        }
    }
}