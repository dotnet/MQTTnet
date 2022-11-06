// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Internal
{
    public sealed class AsyncQueue<TItem> : IDisposable
    {
        readonly AsyncSignal _signal = new AsyncSignal();
        readonly object _syncRoot = new object();

        ConcurrentQueue<TItem> _queue = new ConcurrentQueue<TItem>();

        bool _isDisposed;
        
        public int Count => _queue.Count;

        public void Clear()
        {
            Interlocked.Exchange(ref _queue, new ConcurrentQueue<TItem>());
        }

        public void Dispose()
        {
            lock (_syncRoot)
            {
                _signal?.Dispose();

                _isDisposed = true;
            }
        }

        public void Enqueue(TItem item)
        {
            lock (_syncRoot)
            {
                _queue.Enqueue(item);
                _signal.Set();
            }
        }

        public AsyncQueueDequeueResult<TItem> TryDequeue()
        {
            if (_queue.TryDequeue(out var item))
            {
                return new AsyncQueueDequeueResult<TItem>(true, item);
            }

            return new AsyncQueueDequeueResult<TItem>(false, default);
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
                        if (_isDisposed)
                        {
                            return new AsyncQueueDequeueResult<TItem>(false, default);
                        }

                        task = _signal.WaitAsync(cancellationToken);
                    }

                    await task.ConfigureAwait(false);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return new AsyncQueueDequeueResult<TItem>(false, default);
                    }

                    if (_queue.TryDequeue(out var item))
                    {
                        return new AsyncQueueDequeueResult<TItem>(true, item);
                    }
                }
                catch (OperationCanceledException)
                {
                    return new AsyncQueueDequeueResult<TItem>(false, default);
                }
            }

            return new AsyncQueueDequeueResult<TItem>(false, default);
        }
    }
}