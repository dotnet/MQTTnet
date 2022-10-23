// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
        public async Task Synchronize_100_Tasks()
        {
            const int tasksCount = 100;

            var tasks = new Task[tasksCount];
            var asyncLock = new AsyncLock();
            var globalI = 0;

            for (var i = 0; i < tasksCount; i++)
            {
                tasks[i] = Task.Run(
                    async () =>
                    {
                        using (await asyncLock.EnterAsync().ConfigureAwait(false))
                        {
                            var localI = globalI;
                            await Task.Delay(5).ConfigureAwait(false); // Increase the chance for wrong data.
                            localI++;
                            globalI = localI;
                        }
                    });
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            if (globalI != tasksCount)
            {
                throw new Exception($"Code is broken ({globalI})!");
            }
        }
        
        [Benchmark]
        public async Task Wait_100_000_Times()
        {
            var asyncLock = new AsyncLock();

            using (var cancellationToken = new CancellationTokenSource())
            {
                for (var i = 0; i < 100000; i++)
                {
                    using (await asyncLock.EnterAsync(cancellationToken.Token).ConfigureAwait(false))
                    {
                    }
                }
            }
        }
    }
}