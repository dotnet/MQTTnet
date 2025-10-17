// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore;

public class SocketAwaitable(PipeScheduler ioScheduler) : ICriticalNotifyCompletion
{
    static readonly Action CallbackCompleted = () =>
    {
    };

    int _bytesTransferred;

    Action _callback;
    SocketError _error;

    public bool IsCompleted => ReferenceEquals(_callback, CallbackCompleted);

    public void Complete(int bytesTransferred, SocketError socketError)
    {
        _error = socketError;
        _bytesTransferred = bytesTransferred;
        var continuation = Interlocked.Exchange(ref _callback, CallbackCompleted);

        if (continuation != null)
        {
            ioScheduler.Schedule(state => ((Action)state)(), continuation);
        }
    }

    public SocketAwaitable GetAwaiter()
    {
        return this;
    }

    public int GetResult()
    {
        Debug.Assert(ReferenceEquals(_callback, CallbackCompleted));

        _callback = null;

        if (_error != SocketError.Success)
        {
            throw new SocketException((int)_error);
        }

        return _bytesTransferred;
    }

    public void OnCompleted(Action continuation)
    {
        if (ReferenceEquals(_callback, CallbackCompleted) || ReferenceEquals(Interlocked.CompareExchange(ref _callback, continuation, null), CallbackCompleted))
        {
            Task.Run(continuation);
        }
    }

    public void UnsafeOnCompleted(Action continuation)
    {
        OnCompleted(continuation);
    }
}