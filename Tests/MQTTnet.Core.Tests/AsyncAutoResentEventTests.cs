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
        private readonly AsyncAutoResetEvent _aare;

        public AsyncAutoResetEventTests()
        {
            _aare = new AsyncAutoResetEvent();
        }

        [TestMethod]
        public async Task SingleThreadedPulse()
        {
            for (int i = 0; i < 5; i++)
            {
                var t = _aare.WaitOneAsync();
                Assert.IsFalse(t.IsCompleted);
                _aare.Set();
                await t;
                Assert.IsTrue(t.IsCompleted);
            }
        }

        [TestMethod]
        public async Task MultipleSetOnlySignalsOnce()
        {
            _aare.Set();
            _aare.Set();
            await _aare.WaitOneAsync();
            var t = _aare.WaitOneAsync();
            Assert.IsFalse(t.IsCompleted);
            await Task.Delay(500);
            Assert.IsFalse(t.IsCompleted);
            _aare.Set();
            await t;
            Assert.IsTrue(t.IsCompleted);
        }

        [TestMethod]
        public async Task OrderPreservingQueue()
        {
            var waiters = new Task[5];
            for (int i = 0; i < waiters.Length; i++)
            {
                waiters[i] = _aare.WaitOneAsync();
            }

            for (int i = 0; i < waiters.Length; i++)
            {
                _aare.Set();
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
            var inlinedContinuation = _aare.WaitOneAsync()
                .ContinueWith(delegate
                {
                    // Arrange to synchronously block the continuation until Set() has returned,
                    // which would deadlock if Set does not return until inlined continuations complete.
                    Assert.IsTrue(setReturned.Wait(500));
                });
            await Task.Delay(100);
            _aare.Set();
            setReturned.Set();
            Assert.IsTrue(inlinedContinuation.Wait(500));
        }

        [TestMethod]
        public void WaitAsync_WithCancellationToken()
        {
            var cts = new CancellationTokenSource();
            Task waitTask = _aare.WaitOneAsync(cts.Token);
            Assert.IsFalse(waitTask.IsCompleted);

            // Cancel the request and ensure that it propagates to the task.
            cts.Cancel();
            try
            {
                waitTask.GetAwaiter().GetResult();
                Assert.IsTrue(false, "Task was expected to transition to a canceled state.");
            }
            catch (OperationCanceledException ex)
            {
                Assert.AreEqual(cts.Token, ex.CancellationToken);
            }

            // Now set the event and verify that a future waiter gets the signal immediately.
            _aare.Set();
            waitTask = _aare.WaitOneAsync();
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
            _aare.Set();
            try
            {
                _aare.WaitOneAsync(token).GetAwaiter().GetResult();
                Assert.IsTrue(false, "Task was expected to transition to a canceled state.");
            }
            catch (OperationCanceledException ex)
            {
                Assert.AreEqual(token, ex.CancellationToken);
            }

            // Verify that the signal was not acquired.
            Task waitTask = _aare.WaitOneAsync();
            Assert.AreEqual(TaskStatus.RanToCompletion, waitTask.Status);
        }

        [TestMethod]
        public async Task WaitAsync_WithTimeout()
        {
            Task waitTask = _aare.WaitOneAsync(TimeSpan.FromMilliseconds(500));
            Assert.IsFalse(waitTask.IsCompleted);

            // Cancel the request and ensure that it propagates to the task.
            await Task.Delay(1000).ConfigureAwait(false);
            try
            {
                waitTask.GetAwaiter().GetResult();
                Assert.IsTrue(false, "Task was expected to transition to a timeout state.");
            }
            catch (TimeoutException)
            {
                Assert.IsTrue(true);
            }

            // Now set the event and verify that a future waiter gets the signal immediately.
            _aare.Set();
            waitTask = _aare.WaitOneAsync(TimeSpan.FromMilliseconds(500));
            Assert.AreEqual(TaskStatus.RanToCompletion, waitTask.Status);
        }

        [TestMethod]
        public void WaitAsync_Canceled_DoesNotInlineContinuations()
        {
            var cts = new CancellationTokenSource();
            var task = _aare.WaitOneAsync(cts.Token);

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