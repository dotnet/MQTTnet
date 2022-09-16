// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MQTTnet.Internal;

namespace MQTTnet.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net60)]
    [MemoryDiagnoser]
    public class AsyncLockBenchmark : BaseBenchmark
    {
        [Benchmark]
        public async Task Wait_100_000_Times()
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