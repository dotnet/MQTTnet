// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Internal;

namespace MQTTnet.Tests.Internal;

// ReSharper disable InconsistentNaming
[TestClass]
public sealed class AsyncLock_Tests
{
    [TestMethod]
    public async Task Cancellation_Of_Awaiter()
    {
        var @lock = new AsyncLock();

        // This call will not yet "release" the lock due to missing _using_.
        var releaser = await @lock.EnterAsync().ConfigureAwait(false);

        var counter = 0;

        Debug.WriteLine("Prepared locked lock.");

        _ = Task.Run(
            async () =>
            {
                // SHOULD GET TIMEOUT!
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                using (await @lock.EnterAsync(timeout.Token))
                {
                    Debug.WriteLine("Task 1 incremented");
                    counter++;
                }
            });

        _ = Task.Run(
            async () =>
            {
                // SHOULD GET TIMEOUT!
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                using (await @lock.EnterAsync(timeout.Token))
                {
                    Debug.WriteLine("Task 2 incremented");
                    counter++;
                }
            });

        _ = Task.Run(
            async () =>
            {
                // SHOULD GET TIMEOUT!
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                using (await @lock.EnterAsync(timeout.Token))
                {
                    Debug.WriteLine("Task 3 incremented");
                    counter++;
                }
            });

        _ = Task.Run(
            async () =>
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(4));
                using (await @lock.EnterAsync(timeout.Token))
                {
                    Debug.WriteLine("Task 4 incremented");
                    counter++;
                }
            });

        _ = Task.Run(
            async () =>
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                using (await @lock.EnterAsync(timeout.Token))
                {
                    Debug.WriteLine("Task 5 incremented");
                    counter++;
                }
            });

        _ = Task.Run(
            async () =>
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(6));
                using (await @lock.EnterAsync(timeout.Token))
                {
                    Debug.WriteLine("Task 6 incremented");
                    counter++;
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

        var @lock = new AsyncLock();

        var tasks = new Task[taskCount];
        var globalI = 0;
        for (var i = 0; i < taskCount; i++)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            tasks[i] = Task.Run(
                async () =>
                {
                    using (await @lock.EnterAsync())
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

        var @lock = new AsyncLock();
        for (var i = 0; i < 100; i++)
        {
            using (await @lock.EnterAsync().ConfigureAwait(false))
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
        await @lock.EnterAsync().ConfigureAwait(false);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await @lock.EnterAsync(cts.Token).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Test_Cancellation_With_Later_Access()
    {
        var asyncLock = new AsyncLock();

        var releaser = await asyncLock.EnterAsync().ConfigureAwait(false);

        try
        {
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3)))
            {
                await asyncLock.EnterAsync(timeout.Token).ConfigureAwait(false);
            }

            Assert.Fail("Exception should be thrown!");
        }
        catch (OperationCanceledException)
        {
        }

        releaser.Dispose();

        using (await asyncLock.EnterAsync(CancellationToken.None).ConfigureAwait(false))
        {
            // When the method finished, the thread got access.
        }
    }

    [TestMethod]
    public async Task Use_After_Cancellation()
    {
        var @lock = new AsyncLock();

        // This call will not yet "release" the lock due to missing _using_.
        var releaser = await @lock.EnterAsync().ConfigureAwait(false);

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await @lock.EnterAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected exception!
        }

        releaser.Dispose();

        // Regular usage after cancellation.
        using (await @lock.EnterAsync().ConfigureAwait(false))
        {
        }

        using (await @lock.EnterAsync().ConfigureAwait(false))
        {
        }

        using (await @lock.EnterAsync().ConfigureAwait(false))
        {
        }
    }
}