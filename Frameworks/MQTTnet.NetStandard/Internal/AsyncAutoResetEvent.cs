using System;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Internal
{
    public sealed class AsyncAutoResetEvent : IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0, 1);

        public Task WaitOneAsync(CancellationToken cancellationToken)
        {
            return _semaphore.WaitAsync(cancellationToken);
        }

        public void Set()
        {
            _semaphore.Release();
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
        }
    }
}
