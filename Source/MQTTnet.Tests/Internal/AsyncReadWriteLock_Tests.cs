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
    public sealed class AsyncReadWriteLock_Tests
    {
        [TestMethod]
        public async Task Cancellation_Of_Write_Awaiter()
        {
            var @lock = new AsyncReadWriteLock();

            // This call will not yet "release" the lock due to missing _using_.
            var releaser = await @lock.EnterWriteAsync().ConfigureAwait(false);

            var counter = 0;

            Debug.WriteLine("Prepared locked lock.");

            _ = Task.Run(
                async () =>
                {
                    // SHOULD GET TIMEOUT!
                    using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1)))
                    {
                        using (await @lock.EnterWriteAsync(timeout.Token))
                        {
                            Debug.WriteLine("Task 1 incremented");
                            counter++;
                        }
                    }
                });

            _ = Task.Run(
                async () =>
                {
                    // SHOULD GET TIMEOUT!
                    using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2)))
                    {
                        using (await @lock.EnterWriteAsync(timeout.Token))
                        {
                            Debug.WriteLine("Task 2 incremented");
                            counter++;
                        }
                    }
                });

            _ = Task.Run(
                async () =>
                {
                    // SHOULD GET TIMEOUT!
                    using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3)))
                    {
                        using (await @lock.EnterWriteAsync(timeout.Token))
                        {
                            Debug.WriteLine("Task 3 incremented");
                            counter++;
                        }
                    }
                });

            _ = Task.Run(
                async () =>
                {
                    using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(4)))
                    {
                        using (await @lock.EnterWriteAsync(timeout.Token))
                        {
                            Debug.WriteLine("Task 4 incremented");
                            counter++;
                        }
                    }
                });

            _ = Task.Run(
                async () =>
                {
                    using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                    {
                        using (await @lock.EnterWriteAsync(timeout.Token))
                        {
                            Debug.WriteLine("Task 5 incremented");
                            counter++;
                        }
                    }
                });

            _ = Task.Run(
                async () =>
                {
                    using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(6)))
                    {
                        using (await @lock.EnterWriteAsync(timeout.Token))
                        {
                            Debug.WriteLine("Task 6 incremented");
                            counter++;
                        }
                    }
                });

            Debug.WriteLine("Delay before release...");
            await Task.Delay(TimeSpan.FromSeconds(3.1));
            releaser.Dispose();

            Debug.WriteLine("Wait for all tasks...");
            await Task.Delay(TimeSpan.FromSeconds(6.1));

            Assert.AreEqual(3, counter);
        }

        [TestMethod]
        public async Task Cancellation_Of_Read_Awaiter()
        {
            var @lock = new AsyncReadWriteLock();

            // This call will not yet "release" the lock due to missing _using_.
            var releaser = await @lock.EnterWriteAsync().ConfigureAwait(false);

            var counter = 0;
            var syncLock = new object();

            Debug.WriteLine("Prepared locked lock.");

            _ = Task.Run(
                async () =>
                {
                    // SHOULD GET TIMEOUT!
                    using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1)))
                    {
                        using (await @lock.EnterReadAsync(timeout.Token))
                        {
                            Debug.WriteLine("Task 1 incremented");
                            lock (syncLock)
                                counter++;
                        }
                    }
                });

            _ = Task.Run(
                async () =>
                {
                    // SHOULD GET TIMEOUT!
                    using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2)))
                    {
                        using (await @lock.EnterReadAsync(timeout.Token))
                        {
                            Debug.WriteLine("Task 2 incremented");
                            lock (syncLock)
                                counter++;
                        }
                    }
                });

            _ = Task.Run(
                async () =>
                {
                    // SHOULD GET TIMEOUT!
                    using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3)))
                    {
                        using (await @lock.EnterReadAsync(timeout.Token))
                        {
                            Debug.WriteLine("Task 3 incremented");
                            lock (syncLock)
                                counter++;
                        }
                    }
                });

            _ = Task.Run(
                async () =>
                {
                    using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(4)))
                    {
                        using (await @lock.EnterReadAsync(timeout.Token))
                        {
                            Debug.WriteLine("Task 4 incremented");
                            lock (syncLock)
                                counter++;
                        }
                    }
                });

            _ = Task.Run(
                async () =>
                {
                    using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                    {
                        using (await @lock.EnterReadAsync(timeout.Token))
                        {
                            Debug.WriteLine("Task 5 incremented");
                            lock (syncLock)
                                counter++;
                        }
                    }
                });

            _ = Task.Run(
                async () =>
                {
                    using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(6)))
                    {
                        using (await @lock.EnterReadAsync(timeout.Token))
                        {
                            Debug.WriteLine("Task 6 incremented");
                            lock (syncLock)
                                counter++;
                        }
                    }
                });

            Debug.WriteLine("Delay before release...");
            await Task.Delay(TimeSpan.FromSeconds(3.1));
            releaser.Dispose();

            Debug.WriteLine("Wait for all tasks...");
            await Task.Delay(TimeSpan.FromSeconds(6.1));

            Assert.AreEqual(3, counter);
        }

        [TestMethod]
        public void Lock_Parallel_Tasks()
        {
            const int taskCount = 50;
            
            var @lock = new AsyncReadWriteLock();
            
            var tasks = new Task[taskCount];
            var globalI = 0;
            for (var i = 0; i < taskCount; i++)
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                tasks[i] = Task.Run(
                    async () =>
                    {
                        using (await @lock.EnterWriteAsync())
                        {
                            var localI = globalI;
                            await Task.Delay(5); // Increase the chance for wrong data.
                            localI++;
                            globalI = localI;
                        }
                    });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }

            Task.WaitAll(tasks);
            Assert.AreEqual(taskCount, globalI);
        }

        [TestMethod]
        public void Lock_Parallel_Write_Tasks_During_Read()
        {
            const int taskCount = 50;

            var @lock = new AsyncReadWriteLock();

            var tasks = new Task[taskCount * 5];
            var globalWrittenI = 0;
            var globalReadI = 0;
            var globalParallelI = 0;
            var readSync = new object();

            for (var i = 0; i < taskCount; i++)
            {
                tasks[i] = Task.Run(
                    async () =>
                    {
                        using (await @lock.EnterReadAsync())
                        {
                            var localParallelI = globalParallelI;
                            lock (readSync)
                                globalReadI++;
                            await Task.Delay(5 * taskCount); // Increase the chance for wrong data.
                            localParallelI++;
                            globalParallelI = localParallelI;
                        }
                    });
            }

            for (var i = 0; i < taskCount; i++)
            {
                tasks[i + taskCount] = Task.Run(
                    async () =>
                    {
                        using (await @lock.EnterWriteAsync())
                        {
                            var localI = globalWrittenI;
                            await Task.Delay(5); // Increase the chance for wrong data.
                            localI++;
                            globalWrittenI = localI;
                        }
                    });
            }

            for (var i = 0; i < taskCount; i++)
            {
                tasks[i + taskCount * 2] = Task.Run(
                    async () =>
                    {
                        using (await @lock.EnterReadAsync())
                        {
                            var localParallelI = globalParallelI;
                            lock (readSync)
                                globalReadI++;
                            await Task.Delay(5 * taskCount); // Increase the chance for wrong data.
                            localParallelI++;
                            globalParallelI = localParallelI;
                        }
                    });
            }

            for (var i = 0; i < taskCount; i++)
            {
                tasks[i + taskCount * 3] = Task.Run(
                    async () =>
                    {
                        using (await @lock.EnterWriteAsync())
                        {
                            var localI = globalWrittenI;
                            await Task.Delay(5); // Increase the chance for wrong data.
                            localI++;
                            globalWrittenI = localI;
                        }
                    });
            }

            for (var i = 0; i < taskCount; i++)
            {
                tasks[i + taskCount * 4] = Task.Run(
                    async () =>
                    {
                        using (await @lock.EnterReadAsync())
                        {
                            var localParallelI = globalParallelI;
                            lock (readSync)
                                globalReadI++;
                            await Task.Delay(5 * taskCount); // Increase the chance for wrong data.
                            localParallelI++;
                            globalParallelI = localParallelI;
                        }
                    });
            }

            Task.WaitAll(tasks);
            Assert.AreEqual(taskCount * 2, globalWrittenI);
            Assert.AreEqual(taskCount * 3, globalReadI); // Validates that all reads occurred.
            Assert.AreNotEqual(taskCount * 3, globalParallelI); // Ensures the reads happened in parallel.
        }

        [TestMethod]
        public void Lock_10_Parallel_Tasks_With_Dispose_Doesnt_Lockup()
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
                            using (await @lock.EnterAsync())
                            {
                                var localI = globalI;
                                await Task.Delay(10); // Increase the chance for wrong data.
                                localI++;
                                globalI = localI;
                            }
                        })
                    .ContinueWith(
                        x =>
                        {
                            if (globalI == 5)
                            {
                                @lock.Dispose();
                                @lock = new AsyncLock();
                            }

                            if (x.Exception != null)
                            {
                                Debug.WriteLine(x.Exception.GetBaseException().GetType().Name);
                            }
                        });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }

            Task.WaitAll(threads);

            // Expect only 6 because the others are failing due to disposal (if (globalI == 5)).
            Assert.AreEqual(6, globalI);
        }

        [TestMethod]
        public async Task Lock_Serial_Calls()
        {
            var sum = 0;

            var @lock = new AsyncReadWriteLock();
            for (var i = 0; i < 100; i++)
            {
                using (await @lock.EnterWriteAsync().ConfigureAwait(false))
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
            var @lock = new AsyncReadWriteLock();

            // This call will never "release" the lock due to missing _using_.
            await @lock.EnterWriteAsync().ConfigureAwait(false);

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3)))
            {
                await @lock.EnterWriteAsync(cts.Token).ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task Test_Cancellation_With_Later_Access()
        {
            var asyncLock = new AsyncReadWriteLock();

            var releaser = await asyncLock.EnterWriteAsync().ConfigureAwait(false);

            try
            {
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3)))
                {
                    await asyncLock.EnterWriteAsync(timeout.Token).ConfigureAwait(false);
                }

                Assert.Fail("Exception should be thrown!");
            }
            catch (OperationCanceledException)
            {
            }

            releaser.Dispose();

            using (await asyncLock.EnterWriteAsync(CancellationToken.None).ConfigureAwait(false))
            {
                // When the method finished, the thread got access.
            }
        }

        [TestMethod]
        public async Task Use_After_Cancellation()
        {
            var @lock = new AsyncReadWriteLock();

            // This call will not yet "release" the lock due to missing _using_.
            var releaser = await @lock.EnterWriteAsync().ConfigureAwait(false);

            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3)))
                {
                    await @lock.EnterWriteAsync(cts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected exception!
            }

            releaser.Dispose();

            // Regular usage after cancellation.
            using (await @lock.EnterWriteAsync().ConfigureAwait(false))
            {
            }

            using (await @lock.EnterWriteAsync().ConfigureAwait(false))
            {
            }

            using (await @lock.EnterWriteAsync().ConfigureAwait(false))
            {
            }
        }

    }
}