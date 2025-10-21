// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using MQTTnet.Internal;

namespace MQTTnet.Tests.Internal;

// ReSharper disable InconsistentNaming
[TestClass]
public sealed class AsyncSignal_Tests
{
    [TestMethod]
    public Task Cancel_If_No_Signal()
    {
        return Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
        {
            var asyncSignal = new AsyncSignal();

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await asyncSignal.WaitAsync(timeout.Token);

            Assert.Fail("There is no signal. So we must fail here!");
        });
    }

    [TestMethod]
    public Task Dispose_Properly()
    {
        return Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () =>
        {
            var asyncSignal = new AsyncSignal();

            // The timeout will not be reached but another task will kill the async signal.
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(999));
            _ = Task.Run(
                async () =>
                {
                    await Task.Delay(2000, CancellationToken.None);
                    asyncSignal.Dispose();
                },
                CancellationToken.None);

            await asyncSignal.WaitAsync(timeout.Token);

            Assert.Fail("There is no signal. So we must fail here!");
        });
    }

    [TestMethod]
    public async Task Reset_Signal()
    {
        var asyncSignal = new AsyncSignal();

        // WaitAsync should fail because no signal is available.
        for (var i = 0; i < 10; i++)
        {
            try
            {
                using (var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(100)))
                {
                    await asyncSignal.WaitAsync(timeout.Token);
                }

                Assert.Fail("This must fail because the signal is not yet set.");
            }
            catch (OperationCanceledException)
            {
            }

            asyncSignal.Set();

            // WaitAsync should return directly because the signal is available.
            await asyncSignal.WaitAsync();
        }
    }

    [TestMethod]
    public async Task Signal()
    {
        var asyncSignal = new AsyncSignal();

        // The timeout will not be reached but another task will kill the async signal.
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var stopwatch = Stopwatch.StartNew();

        _ = Task.Run(
            async () =>
            {
                await Task.Delay(1000, CancellationToken.None);
                asyncSignal.Set();
            },
            CancellationToken.None);

        await asyncSignal.WaitAsync(timeout.Token);

        stopwatch.Stop();

        Assert.IsGreaterThan(900, stopwatch.ElapsedMilliseconds);
    }

    [TestMethod]
    public Task Fail_For_Two_Waiters()
    {
        return Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
        {
            var asyncSignal = new AsyncSignal();

            // This thread will wait properly because it is the first waiter.
            _ = Task.Run(
                async () =>
                {
                    await asyncSignal.WaitAsync();
                },
                CancellationToken.None);

            await Task.Delay(1000);

            // Now the current thread must fail because there is already a waiter.
            await asyncSignal.WaitAsync();
        });
    }
}