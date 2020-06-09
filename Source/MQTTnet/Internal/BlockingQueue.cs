using System;
using System.Collections.Generic;
using System.Threading;

namespace MQTTnet.Internal
{
    public sealed class BlockingQueue<TItem> : IDisposable
    {
        readonly object _syncRoot = new object();
        readonly LinkedList<TItem> _items = new LinkedList<TItem>();

        ManualResetEventSlim _gate = new ManualResetEventSlim(false);

        public int Count
        {
            get
            {
                lock (_syncRoot)
                {
                    return _items.Count;
                }
            }
        }

        public void Enqueue(TItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            lock (_syncRoot)
            {
                _items.AddLast(item);
                _gate?.Set();
            }
        }

        public TItem Dequeue(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                lock (_syncRoot)
                {
                    if (_items.Count > 0)
                    {
                        var item = _items.First.Value;
                        _items.RemoveFirst();

                        return item;
                    }

                    if (_items.Count == 0)
                    {
                        _gate?.Reset();
                    }
                }

                _gate?.Wait(cancellationToken);
            }

            throw new OperationCanceledException();
        }

        public TItem PeekAndWait(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                lock (_syncRoot)
                {
                    if (_items.Count > 0)
                    {
                        return _items.First.Value;
                    }

                    if (_items.Count == 0)
                    {
                        _gate?.Reset();
                    }
                }

                _gate?.Wait(cancellationToken);
            }

            throw new OperationCanceledException();
        }

        public void RemoveFirst(Predicate<TItem> match)
        {
            if (match == null) throw new ArgumentNullException(nameof(match));

            lock (_syncRoot)
            {
                if (_items.Count > 0 && match(_items.First.Value))
                {
                    _items.RemoveFirst();
                }
            }
        }

        public TItem RemoveFirst()
        {
            lock (_syncRoot)
            {
                var item = _items.First;
                _items.RemoveFirst();

                return item.Value;
            }
        }

        public void Clear()
        {
            lock (_syncRoot)
            {
                _items.Clear();
            }
        }

        public void Dispose()
        {
            _gate?.Dispose();
            _gate = null;
        }
    }
}
