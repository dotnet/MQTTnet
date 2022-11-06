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
    public sealed class AsyncSignal_Tests
    {
        [TestMethod]
        [ExpectedException(typeof(TaskCanceledException))]
        public async Task Cancel_If_No_Signal()
        {
            var asyncSignal = new AsyncSignal();

            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2)))
            {
                await asyncSignal.WaitAsync(timeout.Token);

                Assert.Fail("There is no signal. So we must fail here!");
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public async Task Dispose_Properly()
        {
            var asyncSignal = new AsyncSignal();

            // The timeout will not be reached but another task will kill the async signal.
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(999)))
            {
                _ = Task.Run(
                    async () =>
                    {
                        await Task.Delay(2000, CancellationToken.None);
                        asyncSignal.Dispose();
                    },
                    CancellationToken.None);

                await asyncSignal.WaitAsync(timeout.Token);

                Assert.Fail("There is no signal. So we must fail here!");
            }
        }

        [TestMethod]
        public async Task Loop_Signal()
        {
            var asyncSignal = new AsyncSignal();

            for (var i = 0; i < 10; i++)
            {
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
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3)))
            {
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

                Assert.IsTrue(stopwatch.ElapsedMilliseconds > 900);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task Fail_For_Two_Waiters()
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
        }
    }
}