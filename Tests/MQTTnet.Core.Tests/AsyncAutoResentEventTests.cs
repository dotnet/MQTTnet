using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Internal;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Core.Tests
{
    [TestClass]
    // Inspired from the vs-threading tests (https://github.com/Microsoft/vs-threading/blob/master/src/Microsoft.VisualStudio.Threading.Tests/AsyncAutoResetEventTests.cs)
    public class AsyncAutoResetEventTests
    {
        private readonly AsyncAutoResetEvent evt;

        public AsyncAutoResetEventTests()
        {
            this.evt = new AsyncAutoResetEvent();
        }

        [TestMethod]
        public async Task SingleThreadedPulse()
        {
            for (int i = 0; i < 5; i++)
            {
                var t = this.evt.WaitOneAsync();
                Assert.IsFalse(t.IsCompleted);
                this.evt.Set();
                await t;
                Assert.IsTrue(t.IsCompleted);
            }
        }

        [TestMethod]
        public async Task MultipleSetOnlySignalsOnce()
        {
            this.evt.Set();
            this.evt.Set();
            await this.evt.WaitOneAsync();
            var t = this.evt.WaitOneAsync();
            Assert.IsFalse(t.IsCompleted);
            await Task.Delay(500);
            Assert.IsFalse(t.IsCompleted);
            this.evt.Set();
            await t;
            Assert.IsTrue(t.IsCompleted);
        }

        [TestMethod]
        public async Task OrderPreservingQueue()
        {
            var waiters = new Task[5];
            for (int i = 0; i < waiters.Length; i++)
            {
                waiters[i] = this.evt.WaitOneAsync();
            }

            for (int i = 0; i < waiters.Length; i++)
            {
                this.evt.Set();
                await waiters[i].ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Verifies that inlining continuations do not have to complete execution before Set() returns.
        /// </summary>
        [TestMethod]
        public async Task SetReturnsBeforeInlinedContinuations()
        {
            var setReturned = new ManualResetEventSlim();
            var inlinedContinuation = this.evt.WaitOneAsync()
                .ContinueWith(delegate
                {
                    // Arrange to synchronously block the continuation until Set() has returned,
                    // which would deadlock if Set does not return until inlined continuations complete.
                    Assert.IsTrue(setReturned.Wait(500));
                });
            await Task.Delay(100);
            this.evt.Set();
            setReturned.Set();
            Assert.IsTrue(inlinedContinuation.Wait(500));
        }

        [TestMethod]
        public void WaitAsync_WithCancellationToken()
        {
            var cts = new CancellationTokenSource();
            Task waitTask = this.evt.WaitOneAsync(cts.Token);
            Assert.IsFalse(waitTask.IsCompleted);

            // Cancel the request and ensure that it propagates to the task.
            cts.Cancel();
            try
            {
                waitTask.GetAwaiter().GetResult();
                Assert.IsTrue(false, "Task was expected to transition to a canceled state.");
            }
            catch (System.OperationCanceledException ex)
            {
                Assert.AreEqual(cts.Token, ex.CancellationToken);
            }

            // Now set the event and verify that a future waiter gets the signal immediately.
            this.evt.Set();
            waitTask = this.evt.WaitOneAsync();
            Assert.AreEqual(TaskStatus.RanToCompletion, waitTask.Status);
        }

        [TestMethod]
        public void WaitAsync_WithCancellationToken_Precanceled()
        {
            // We construct our own pre-canceled token so that we can do
            // a meaningful identity check later.
            var tokenSource = new CancellationTokenSource();
            tokenSource.Cancel();
            var token = tokenSource.Token;

            // Verify that a pre-set signal is not reset by a canceled wait request.
            this.evt.Set();
            try
            {
                this.evt.WaitOneAsync(token).GetAwaiter().GetResult();
                Assert.IsTrue(false, "Task was expected to transition to a canceled state.");
            }
            catch (OperationCanceledException ex)
            {
                Assert.AreEqual(token, ex.CancellationToken);
            }

            // Verify that the signal was not acquired.
            Task waitTask = this.evt.WaitOneAsync();
            Assert.AreEqual(TaskStatus.RanToCompletion, waitTask.Status);
        }

        [TestMethod]
        public async Task WaitAsync_WithTimeout()
        {
            Task waitTask = this.evt.WaitOneAsync(TimeSpan.FromMilliseconds(500));
            Assert.IsFalse(waitTask.IsCompleted);

            // Cancel the request and ensure that it propagates to the task.
            await Task.Delay(1000).ConfigureAwait(false);
            try
            {
                waitTask.GetAwaiter().GetResult();
                Assert.IsTrue(false, "Task was expected to transition to a timeout state.");
            }
            catch (System.TimeoutException)
            {
                Assert.IsTrue(true);
            }

            // Now set the event and verify that a future waiter gets the signal immediately.
            this.evt.Set();
            waitTask = this.evt.WaitOneAsync(TimeSpan.FromMilliseconds(500));
            Assert.AreEqual(TaskStatus.RanToCompletion, waitTask.Status);
        }

        [TestMethod]
        public void WaitAsync_Canceled_DoesNotInlineContinuations()
        {
            var cts = new CancellationTokenSource();
            var task = this.evt.WaitOneAsync(cts.Token);

            var completingActionFinished = new ManualResetEventSlim();
            var continuation = task.ContinueWith(
                _ => Assert.IsTrue(completingActionFinished.Wait(500)),
                CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default);

            cts.Cancel();
            completingActionFinished.Set();

            // Rethrow the exception if it turned out it deadlocked.
            continuation.GetAwaiter().GetResult();
        }

        [TestMethod]
        public async Task AsyncAutoResetEvent()
        {
            var aare = new AsyncAutoResetEvent();

            var globalI = 0;
#pragma warning disable 4014
            Task.Run(async () =>
#pragma warning restore 4014
            {
                await aare.WaitOneAsync(CancellationToken.None);
                globalI += 1;
            });

#pragma warning disable 4014
            Task.Run(async () =>
#pragma warning restore 4014
            {
                await aare.WaitOneAsync(CancellationToken.None);
                globalI += 2;
            });

            await Task.Delay(500);
            aare.Set();
            await Task.Delay(500);
            aare.Set();
            await Task.Delay(100);

            Assert.AreEqual(3, globalI);
        }
    }
}