using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Internal
{
    public sealed class AsyncQueue<TItem> : IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);

        private ConcurrentQueue<TItem> _queue = new ConcurrentQueue<TItem>();

        public int Count => _queue.Count;

        public void Enqueue(TItem item)
        {
            _queue.Enqueue(item);
            _semaphore.Release();
        }

        public async Task<AsyncQueueDequeueResult<TItem>> TryDequeueAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                if (_queue.TryDequeue(out var item))
                {
                    return new AsyncQueueDequeueResult<TItem>(true, item);
                }
            }

            return new AsyncQueueDequeueResult<TItem>(false, default(TItem));
        }

        public AsyncQueueDequeueResult<TItem> TryDequeue()
        {
            if (_queue.TryDequeue(out var item))
            {
                return new AsyncQueueDequeueResult<TItem>(true, item);
            }

            return new AsyncQueueDequeueResult<TItem>(false, default(TItem));
        }

        public void Clear()
        {
            Interlocked.Exchange(ref _queue, new ConcurrentQueue<TItem>());
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
        }
    }
}
