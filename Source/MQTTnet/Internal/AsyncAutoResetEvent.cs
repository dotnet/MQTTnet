using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Internal
{
    // Inspired from Stephen Toub (https://blogs.msdn.microsoft.com/pfxteam/2012/02/11/building-async-coordination-primitives-part-2-asyncautoresetevent/) and Chris Gillum (https://stackoverflow.com/a/43012490)
    public class AsyncAutoResetEvent
    {
        private readonly LinkedList<TaskCompletionSource<bool>> _waiters = new LinkedList<TaskCompletionSource<bool>>();

        private bool _isSignaled;

        public AsyncAutoResetEvent()
            : this(false)
        {
        }

        public AsyncAutoResetEvent(bool signaled)
        {
            _isSignaled = signaled;
        }

        public int WaitersCount
        {
            get
            {
                lock (_waiters)
                {
                    return _waiters.Count;
                }
            }
        }

        public Task<bool> WaitOneAsync()
        {
            return WaitOneAsync(CancellationToken.None);
        }

        public Task<bool> WaitOneAsync(TimeSpan timeout)
        {
            return WaitOneAsync(timeout, CancellationToken.None);
        }

        public Task<bool> WaitOneAsync(CancellationToken cancellationToken)
        {
            return WaitOneAsync(Timeout.InfiniteTimeSpan, cancellationToken);
        }

        public async Task<bool> WaitOneAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            TaskCompletionSource<bool> tcs;

            lock (_waiters)
            {
                if (_isSignaled)
                {
                    _isSignaled = false;
                    return true;
                }

                if (timeout == TimeSpan.Zero)
                {
                    return _isSignaled;
                }

                tcs = new TaskCompletionSource<bool>();
                _waiters.AddLast(tcs);
            }

            Task winner;
            if (timeout == Timeout.InfiniteTimeSpan)
            {
                await tcs.Task.ConfigureAwait(false);
                winner = tcs.Task;
            }
            else
            {
                winner = await Task.WhenAny(tcs.Task, Task.Delay(timeout, cancellationToken)).ConfigureAwait(false);
            }
            
            var taskWasSignaled = winner == tcs.Task;
            if (taskWasSignaled)
            {
                return true;
            }

            // We timed-out; remove our reference to the task.
            // This is an O(n) operation since waiters is a LinkedList<T>.
            lock (_waiters)
            {
                _waiters.Remove(tcs);

                if (winner.Status == TaskStatus.Canceled)
                {
                    throw new OperationCanceledException(cancellationToken);
                }

                throw new TimeoutException();
            }
        }

        public void Set()
        {
            TaskCompletionSource<bool> toRelease = null;

            lock (_waiters)
            {
                if (_waiters.Count > 0)
                {
                    // Signal the first task in the waiters list.
                    toRelease = _waiters.First.Value;
                    _waiters.RemoveFirst();
                }
                else if (!_isSignaled)
                {
                    // No tasks are pending
                    _isSignaled = true;
                }
            }

            toRelease?.SetResult(true);
        }
    }
}
