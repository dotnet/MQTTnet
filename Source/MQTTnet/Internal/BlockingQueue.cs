// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace MQTTnet.Internal;

public sealed class BlockingQueue<TItem> : IDisposable
{
    readonly object _syncRoot = new();
    readonly LinkedList<TItem> _items = [];

    ManualResetEventSlim _gate = new(false);

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
        ArgumentNullException.ThrowIfNull(item);

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
                    var item = _items.First!.Value;
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
                    return _items.First!.Value;
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
        ArgumentNullException.ThrowIfNull(match);

        lock (_syncRoot)
        {
            if (_items.Count > 0 && match(_items.First!.Value))
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

            return item!.Value;
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