using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Internal
{
    // TODO: System.Threading.Channels.Channel would be a better fit here, once 452 is dropped
    public sealed class AsyncQueue<TItem> : IDisposable
    {
        readonly object _syncRoot = new object();
        readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);
        
        ConcurrentQueue<TItem> _queue = new ConcurrentQueue<TItem>();

        public int Count => _queue.Count;

        public void Enqueue(TItem item)
        {
            lock (_syncRoot)
            {
                _queue.Enqueue(item);
                _semaphore.Release();
            }
        }

        public async Task<AsyncQueueDequeueResult<TItem>> TryDequeueAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Task task;
                    lock (_syncRoot)
                    {
                        if (_semaphore == null)
                        {
                            return new AsyncQueueDequeueResult<TItem>(false, default);
                        }

                        task = _semaphore.WaitAsync(cancellationToken);
                    }
                    
                    await task.ConfigureAwait(false);

                    if (_queue.TryDequeue(out var item))
                    {
                        return new AsyncQueueDequeueResult<TItem>(true, item);
                    }

                    // need to reset semaphore
                    _semaphore.Release();
                }
                catch (ArgumentNullException)
                {
                    // The semaphore throws this internally sometimes.
                    return new AsyncQueueDequeueResult<TItem>(false, default);
                }
                catch (OperationCanceledException)
                {
                    return new AsyncQueueDequeueResult<TItem>(false, default);
                }
            }

            return new AsyncQueueDequeueResult<TItem>(false, default);
        }

        public AsyncQueueDequeueResult<TItem> TryDequeue()
        {
            if (_semaphore.Wait(0))
            {
                if (_queue.TryDequeue(out var item))
                {
                    return new AsyncQueueDequeueResult<TItem>(true, item);
                }

                // need to reset semaphore
                _semaphore.Release();
            }

            return new AsyncQueueDequeueResult<TItem>(false, default);
        }
        
        public void Clear()
        {
            Interlocked.Exchange(ref _queue, new ConcurrentQueue<TItem>());
        }

        public void Dispose()
        {
            lock (_syncRoot)
            {
                _semaphore.Dispose();
            }
        }
    }
}
