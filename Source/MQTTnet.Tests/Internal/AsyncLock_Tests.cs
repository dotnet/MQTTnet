// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Internal;

namespace MQTTnet.Tests.Internal
{
    [TestClass]
    public class AsyncLock_Tests
    {
        [TestMethod]
        public void Lock_10_Parallel_Tasks()
        {
            const int ThreadsCount = 10;

            var threads = new Task[ThreadsCount];
            var @lock = new AsyncLock();
            var globalI = 0;
            for (var i = 0; i < ThreadsCount; i++)
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                threads[i] = Task.Run(
                    async () =>
                    {
                        using (await @lock.WaitAsync(CancellationToken.None))
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

//         [TestMethod]
//         public void Lock_10_Parallel_Tasks_With_Dispose_Doesnt_Lockup()
//         {
//             const int ThreadsCount = 10;
//
//             var threads = new Task[ThreadsCount];
//             var @lock = new AsyncLock();
//             var globalI = 0;
//             for (var i = 0; i < ThreadsCount; i++)
//             {
// #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
//                 threads[i] = Task.Run(
//                         async () =>
//                         {
//                             using (await @lock.WaitAsync(CancellationToken.None))
//                             {
//                                 var localI = globalI;
//                                 await Task.Delay(10); // Increase the chance for wrong data.
//                                 localI++;
//                                 globalI = localI;
//                             }
//                         })
//                     .ContinueWith(
//                         x =>
//                         {
//                             if (globalI == 5)
//                             {
//                                 @lock.Dispose();
//                                 @lock = new AsyncLock();
//                             }
//
//                             if (x.Exception != null)
//                             {
//                                 Debug.WriteLine(x.Exception.GetBaseException().GetType().Name);
//                             }
//                         });
// #pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
//             }
//
//             Task.WaitAll(threads);
//
//             // Expect only 6 because the others are failing due to disposal (if (globalI == 5)).
//             Assert.AreEqual(6, globalI);
//         }

        [TestMethod]
        public async Task Lock_Serial_Calls()
        {
            var sum = 0;

            var @lock = new AsyncLock();
            for (var i = 0; i < 100; i++)
            {
                using (await @lock.WaitAsync(CancellationToken.None).ConfigureAwait(false))
                {
                    sum++;
                }
            }

            Assert.AreEqual(100, sum);
        }

        [TestMethod]
        [ExpectedException(typeof(TaskCanceledException))]
        public async Task Test_Cancellation()
        {
            var @lock = new AsyncLock();

            // This call will never "release" the lock due to missing _using_.
            await @lock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3)))
            {
                await @lock.WaitAsync(cts.Token).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task Test_Cancellation_With_Later_Access()
        {
            var asyncLock = new AsyncLock();

            var releaser = await asyncLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            try
            {
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3)))
                {
                    await asyncLock.WaitAsync(timeout.Token).ConfigureAwait(false);
                }
                
                Assert.Fail("Exception should be thrown!");
            }
            catch (OperationCanceledException)
            {
            }

            releaser.Dispose();

            using (await asyncLock.WaitAsync(CancellationToken.None).ConfigureAwait(false))
            {
                // When the method finished, the thread got access.
            }
        }
    }
}