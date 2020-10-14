using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Internal
{
    public sealed class AsyncQueue<TItem> : IDisposable
    {
        SemaphoreSlim _semaphore = new SemaphoreSlim(0);
        ConcurrentQueue<TItem> _queue = new ConcurrentQueue<TItem>();
        
        public int Count => _queue.Count;

        public void Enqueue(TItem item)
        {
            _queue.Enqueue(item);
            _semaphore?.Release();
        }

        public async Task<AsyncQueueDequeueResult<TItem>> TryDequeueAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_semaphore == null)
                    {
                        return new AsyncQueueDequeueResult<TItem>(false, default);
                    }

                    await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException)
                {
                    return new AsyncQueueDequeueResult<TItem>(false, default);
                }

                if (_queue.TryDequeue(out var item))
                {
                    return new AsyncQueueDequeueResult<TItem>(true, item);
                }
            }

            return new AsyncQueueDequeueResult<TItem>(false, default);
        }

        public AsyncQueueDequeueResult<TItem> TryDequeue()
        {
            if (_queue.TryDequeue(out var item))
            {
                return new AsyncQueueDequeueResult<TItem>(true, item);
            }

            return new AsyncQueueDequeueResult<TItem>(false, default);
        }

        public void Clear()
        {
            Interlocked.Exchange(ref _queue, new ConcurrentQueue<TItem>());
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
            _semaphore = null;
        }
    }
}
