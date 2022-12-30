using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MQTTnet.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTnet.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net60)]
    [SimpleJob(RuntimeMoniker.Net70)]
    [MemoryDiagnoser]
    public class AsyncReadWriteLockBenchmark : BaseBenchmark
    {

        [Benchmark]
        public async Task Synchronize_100_Read_Tasks()
        {
            const int tasksCount = 100;
            var tasks = new Task[tasksCount];
            var asyncReadWriteLock = new AsyncReadWriteLock();
            
            for (var i = 0; i < tasksCount; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    using (await asyncReadWriteLock.EnterReadAsync().ConfigureAwait(false))
                    {
                        await Task.Delay(5).ConfigureAwait(false);
                    }
                });
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

    }
}
