using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Exceptions;
using MQTTnet.Internal;

namespace MQTTnet.Tests
{
    [TestClass]
    public class Extension_Tests
    {
        [ExpectedException(typeof(MqttCommunicationTimedOutException))]
        [TestMethod]
        public async Task TimeoutAfter()
        {
            await MqttTaskTimeout.WaitAsync(ct => Task.Delay(TimeSpan.FromMilliseconds(500), ct),
                TimeSpan.FromMilliseconds(100), CancellationToken.None);
        }

        [ExpectedException(typeof(MqttCommunicationTimedOutException))]
        [TestMethod]
        public async Task TimeoutAfterWithResult()
        {
            await MqttTaskTimeout.WaitAsync(
                ct => Task.Delay(TimeSpan.FromMilliseconds(500), ct).ContinueWith(t => 5, ct),
                TimeSpan.FromMilliseconds(100), CancellationToken.None);
        }

        [TestMethod]
        public async Task TimeoutAfterCompleteInTime()
        {
            var result = await MqttTaskTimeout.WaitAsync(
                ct => Task.Delay(TimeSpan.FromMilliseconds(100), ct).ContinueWith(t => 5, ct),
                TimeSpan.FromMilliseconds(500), CancellationToken.None);
            Assert.AreEqual(5, result);
        }

        [TestMethod]
        public async Task TimeoutAfterWithInnerException()
        {
            try
            {
                await MqttTaskTimeout.WaitAsync(ct => Task.Run(() =>
                {
                    var iis = new int[0];
                    iis[1] = 0;
                }, ct), TimeSpan.FromSeconds(1), CancellationToken.None);

                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is IndexOutOfRangeException);
            }
        }

        [TestMethod]
        public async Task TimeoutAfterWithInnerExceptionWithResult()
        {
            try
            {
                await MqttTaskTimeout.WaitAsync(ct => Task.Run(() =>
                {
                    var iis = new int[0];
                    iis[1] = 0;
                    return iis[0];
                }, ct), TimeSpan.FromSeconds(1), CancellationToken.None);

                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is IndexOutOfRangeException);
            }
        }

        [TestMethod]
        public async Task TimeoutAfterMemoryUsage()
        {
            var tasks = Enumerable.Range(0, 100000)
                .Select(i =>
                {
                    return MqttTaskTimeout.WaitAsync(ct => Task.Delay(TimeSpan.FromMilliseconds(1), ct),
                        TimeSpan.FromMinutes(1), CancellationToken.None);
                });

            await Task.WhenAll(tasks);
            AssertIsLess(3_000_000, GC.GetTotalMemory(true));
        }

        private static void AssertIsLess(long bound, long actual)
        {
            if (bound < actual)
            {
                Assert.Fail($"value must be less than {bound:N0} but is {actual:N0}");
            }
        }
    }
}