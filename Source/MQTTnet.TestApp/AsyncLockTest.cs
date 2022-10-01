// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Internal;

namespace MQTTnet.TestApp
{
    public sealed class AsyncLockTest
    {
        public async Task Run()
        {
            var asyncLock = new AsyncLock();

            using (var cancellationToken = new CancellationTokenSource())
            {
                for (var i = 0; i < 100000; i++)
                {
                    using (await asyncLock.WaitAsync(cancellationToken.Token).ConfigureAwait(false))
                    {
                    }
                } 
            }
        }
    }
}