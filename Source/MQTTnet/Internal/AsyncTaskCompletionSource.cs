// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace MQTTnet.Internal;

public sealed class AsyncTaskCompletionSource<TResult>
{
    readonly TaskCompletionSource<TResult> _taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<TResult> Task => _taskCompletionSource.Task;

    public void TrySetCanceled()
    {
        _taskCompletionSource.TrySetCanceled();
    }

    public void TrySetException(Exception exception)
    {
        _taskCompletionSource.TrySetException(exception);
    }

    public bool TrySetResult(TResult result)
    {
        return _taskCompletionSource.TrySetResult(result);
    }
}