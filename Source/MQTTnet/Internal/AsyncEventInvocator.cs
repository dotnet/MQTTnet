// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace MQTTnet.Internal;

public readonly struct AsyncEventInvocator<TEventArgs>
{
    readonly Action<TEventArgs>? _handler;
    readonly Func<TEventArgs, Task>? _asyncHandler;

    public AsyncEventInvocator(Action<TEventArgs>? handler, Func<TEventArgs, Task>? asyncHandler)
    {
        _handler = handler;
        _asyncHandler = asyncHandler;
    }

    public bool WrapsHandler(Action<TEventArgs> handler1)
    {
        // Do not use ReferenceEquals! It will not work with delegates.
        return handler1 == _handler;
    }

    public bool WrapsHandler(Func<TEventArgs, Task> handler)
    {
        // Do not use ReferenceEquals! It will not work with delegates.
        return handler == _asyncHandler;
    }

    public Task InvokeAsync(TEventArgs eventArgs)
    {
        if (_handler != null)
        {
            _handler(eventArgs);
            return CompletedTask.Instance;
        }

        if (_asyncHandler != null)
        {
            return _asyncHandler(eventArgs);
        }

        throw new InvalidOperationException();
    }
}