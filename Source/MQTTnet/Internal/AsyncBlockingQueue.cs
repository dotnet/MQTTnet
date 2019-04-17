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

        /// <summary>
        /// try to dequeue an item until successfull or cancelled
        /// </summary>
        public async Task<TItem> DequeueAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                if (_queue.TryDequeue(out var item))
                {
                    return item;
                }
            }

            return default(TItem);
        }

        /// <summary>
        /// try to dequeue an item once
        /// </summary>
        public bool TryDequeue(out TItem item)
        {
            return _queue.TryDequeue(out item);
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
