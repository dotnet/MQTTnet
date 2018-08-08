using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Internal;

namespace MQTTnet.Core.Tests
{
    [TestClass]
    public class AsyncLockTests
    {
        [TestMethod]
        public void AsyncLock()
        {
            const int ThreadsCount = 10;

            var threads = new Task[ThreadsCount];
            var @lock = new AsyncLock();
            var globalI = 0;
            for (var i = 0; i < ThreadsCount; i++)
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                threads[i] = Task.Run(async () =>
                {
                    using (var releaser = await @lock.LockAsync(CancellationToken.None))
                    {
                        var localI = globalI;
                        await Task.Delay(10); // Increase the chance for wrong data.
                        localI++;
                        globalI = localI;
                    }
                });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }

            Task.WaitAll(threads);
            Assert.AreEqual(ThreadsCount, globalI);
        }
    }
}
