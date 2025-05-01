// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Internal;

public sealed class AsyncQueueDequeueResult<TItem>(bool isSuccess, TItem item)
{
    public static readonly AsyncQueueDequeueResult<TItem> NonSuccess = new(false, default);

    public bool IsSuccess { get; } = isSuccess;

    public TItem Item { get; } = item;
}