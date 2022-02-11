// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MQTTnet.Internal
{
    public class AsyncQueueDequeueResult<TItem>
    {
        public AsyncQueueDequeueResult(bool isSuccess, TItem item)
        {
            IsSuccess = isSuccess;
            Item = item;
        }

        public bool IsSuccess { get; }

        public TItem Item { get; }
    }
}