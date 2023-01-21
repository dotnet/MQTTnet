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
#if NETCOREAPP3_1_OR_GREATER
            _queue.Clear();
#else
            Interlocked.Exchange(ref _queue, new ConcurrentQueue<TItem>());
#endif
        }

        public void Dispose()
        {
            lock (_syncRoot)
            {
                _signal.Dispose();

                _isDisposed = true;

#if !NETSTANDARD1_3
                if (typeof(IDisposable).IsAssignableFrom(typeof(TItem)))
                {
                    while (_queue.TryDequeue(out TItem item))
                    {
                        (item as IDisposable).Dispose();
                    }
                }
#endif
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
                return AsyncQueueDequeueResult<TItem>.Success(item);
            }

            return AsyncQueueDequeueResult<TItem>.NonSuccess;
        }

        public async Task<AsyncQueueDequeueResult<TItem>> TryDequeueAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Task task = null;
                    lock (_syncRoot)
                    {
                        if (_isDisposed)
                        {
                            return AsyncQueueDequeueResult<TItem>.NonSuccess;
                        }

                        if (_queue.IsEmpty)
                        {
                            task = _signal.WaitAsync(cancellationToken);
                        }
                    }

                    if (task != null)
                    {
                        await task.ConfigureAwait(false);    
                    }
                    
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return AsyncQueueDequeueResult<TItem>.NonSuccess;
                    }

                    if (_queue.TryDequeue(out var item))
                    {
                        return AsyncQueueDequeueResult<TItem>.Success(item);
                    }
                }
                catch (OperationCanceledException)
                {
                    return AsyncQueueDequeueResult<TItem>.NonSuccess;
                }
            }

            return AsyncQueueDequeueResult<TItem>.NonSuccess;
        }
    }
}