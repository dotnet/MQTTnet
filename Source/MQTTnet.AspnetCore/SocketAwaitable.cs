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

public class SocketAwaitable : ICriticalNotifyCompletion
{
    static readonly Action _callbackCompleted = () =>
    {
    };

    readonly PipeScheduler _ioScheduler;
    int _bytesTransferred;

    Action _callback;
    SocketError _error;

    public SocketAwaitable(PipeScheduler ioScheduler)
    {
        _ioScheduler = ioScheduler;
    }

    public bool IsCompleted => ReferenceEquals(_callback, _callbackCompleted);

    public void Complete(int bytesTransferred, SocketError socketError)
    {
        _error = socketError;
        _bytesTransferred = bytesTransferred;
        var continuation = Interlocked.Exchange(ref _callback, _callbackCompleted);

        if (continuation != null)
        {
            _ioScheduler.Schedule(state => ((Action)state)(), continuation);
        }
    }

    public SocketAwaitable GetAwaiter()
    {
        return this;
    }

    public int GetResult()
    {
        Debug.Assert(ReferenceEquals(_callback, _callbackCompleted));

        _callback = null;

        if (_error != SocketError.Success)
        {
            throw new SocketException((int)_error);
        }

        return _bytesTransferred;
    }

    public void OnCompleted(Action continuation)
    {
        if (ReferenceEquals(_callback, _callbackCompleted) || ReferenceEquals(Interlocked.CompareExchange(ref _callback, continuation, null), _callbackCompleted))
        {
            Task.Run(continuation);
        }
    }

    public void UnsafeOnCompleted(Action continuation)
    {
        OnCompleted(continuation);
    }
}