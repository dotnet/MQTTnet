using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Internal
{
    // Inspired from Stephen Toub (https://blogs.msdn.microsoft.com/pfxteam/2012/02/11/building-async-coordination-primitives-part-2-asyncautoresetevent/) and Chris Gillum (https://stackoverflow.com/a/43012490)
    public class AsyncAutoResetEvent
    {
        private readonly LinkedList<TaskCompletionSource<bool>> waiters = new LinkedList<TaskCompletionSource<bool>>();

        private bool isSignaled;

        public AsyncAutoResetEvent() : this(false)
        { }

        public AsyncAutoResetEvent(bool signaled)
        {
            this.isSignaled = signaled;
        }

        public Task<bool> WaitOneAsync()
        {
            return this.WaitOneAsync(CancellationToken.None);
        }

        public Task<bool> WaitOneAsync(TimeSpan timeout)
        {
            return this.WaitOneAsync(timeout, CancellationToken.None);
        }

        public Task<bool> WaitOneAsync(CancellationToken cancellationToken)
        {
            return this.WaitOneAsync(Timeout.InfiniteTimeSpan, cancellationToken);
        }

        public async Task<bool> WaitOneAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            TaskCompletionSource<bool> tcs;

            lock (this.waiters)
            {
                if (this.isSignaled)
                {
                    this.isSignaled = false;
                    return true;
                }
                else if (timeout == TimeSpan.Zero)
                {
                    return this.isSignaled;
                }
                else
                {
                    tcs = new TaskCompletionSource<bool>();
                    this.waiters.AddLast(tcs);
                }
            }

            Task winner = await Task.WhenAny(tcs.Task, Task.Delay(timeout, cancellationToken)).ConfigureAwait(false);
            if (winner == tcs.Task)
            {
                // The task was signaled.
                return true;
            }
            else
            {
                // We timed-out; remove our reference to the task.
                // This is an O(n) operation since waiters is a LinkedList<T>.
                lock (this.waiters)
                {
                    this.waiters.Remove(tcs);
                    if (winner.Status == TaskStatus.Canceled)
                    {
                        throw new OperationCanceledException(cancellationToken);
                    }
                    else
                    {
                        throw new TimeoutException();
                    }
                }
            }
        }

        public void Set()
        {
            TaskCompletionSource<bool> toRelease = null;

            lock (this.waiters)
            {
                if (this.waiters.Count > 0)
                {
                    // Signal the first task in the waiters list.
                    toRelease = this.waiters.First.Value;
                    this.waiters.RemoveFirst();
                }
                else if (!this.isSignaled)
                {
                    // No tasks are pending
                    this.isSignaled = true;
                }
            }

            if (toRelease != null)
            {
                toRelease.SetResult(true);
            }
        }
    }
}
