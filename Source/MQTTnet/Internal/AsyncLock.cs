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
    public sealed class AsyncLock : IDisposable
    {
        /*
         * This async supporting lock does not support reentrancy!
         */

        readonly List<Releaser> _queuedTasks = new List<Releaser>(64);
        readonly Task<IDisposable> _releaserTaskWithDirectApproval;

        readonly Releaser _releaserWithDirectApproval;

        readonly object _syncRoot = new object();

        bool _isDisposed;
        
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
                
                _isDisposed = true;
            }
        }

        public Task<IDisposable> WaitAsync(CancellationToken cancellationToken)
        {
            var hasDirectApproval = false;
            Releaser releaser;

            lock (_syncRoot)
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(AsyncLock));
                }
                
                if (_queuedTasks.Count == 0)
                {
                    // There is no other waiting task apart from the current one.
                    // So we can approve the current task directly.
                    releaser = _releaserWithDirectApproval;
                    hasDirectApproval = true;
                    Debug.WriteLine("AsyncLock: Task -1 directly approved.");
                }
                else
                {
                    releaser = new Releaser(this, new TaskCompletionSource<IDisposable>(), cancellationToken);
                }

                _queuedTasks.Add(releaser);
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
                
                while (_queuedTasks.Count > 0)
                {
                    var nextTask = _queuedTasks[0];
                    if (!nextTask.IsPending)
                    {
                        // Dequeue all canceled or failed tasks.
                        _queuedTasks.RemoveAt(0);
                        continue;
                    }

                    nextTask.Approve();
                    return;
                }

                Debug.WriteLine("AsyncLock: No Task pending.");
            }
        }

        sealed class Releaser : IDisposable
        {
            readonly AsyncLock _asyncLock;
            readonly CancellationToken _cancellationToken;
            readonly int _id;
            readonly TaskCompletionSource<IDisposable> _promise;

            // ReSharper disable once FieldCanBeMadeReadOnly.Local
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

                _id = promise?.Task.Id ?? -1;

                Debug.WriteLine($"AsyncLock: Task {_id} queued.");
            }

            public bool IsPending => _promise != null && !_promise.Task.IsCanceled && !_promise.Task.IsFaulted && !_promise.Task.IsCompleted;

            public Task<IDisposable> Task => _promise?.Task;

            public void Approve()
            {
                _promise?.TrySetResult(this);

                Debug.WriteLine($"AsyncLock: Task {_id} approved.");
            }

            public void Dispose()
            {
                if (_cancellationToken.CanBeCanceled)
                {
                    _cancellationTokenRegistration.Dispose();
                }

                Debug.WriteLine($"AsyncLock: Task {_id} completed.");

                _asyncLock.Release(this);
            }

            public void Fail(Exception exception)
            {
                _promise?.TrySetException(exception);

                Debug.WriteLine($"AsyncLock: Task {_id} failed ({exception.GetType().Name}).");
            }

            void Cancel()
            {
                _promise?.TrySetCanceled();

                Debug.WriteLine($"AsyncLock: Task {_id} canceled.");
            }
        }
    }
}