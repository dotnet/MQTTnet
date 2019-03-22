using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Internal
{
    public sealed class AsyncQueue<TItem> : IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);
        private readonly ConcurrentQueue<TItem> _queue = new ConcurrentQueue<TItem>();

        public void Enqueue(TItem item)
        {
            _queue.Enqueue(item);
            _semaphore.Release();
        }

        public async Task<TItem> DequeueAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                if (_queue.TryDequeue(out var item))
                {
                    return item;
                }
            }
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
        }
    }
}
