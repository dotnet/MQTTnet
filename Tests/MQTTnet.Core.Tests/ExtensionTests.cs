using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Core.Exceptions;
using MQTTnet.Core.Internal;

namespace MQTTnet.Core.Tests
{
    [TestClass]
    public class ExtensionTests
    {
        [ExpectedException(typeof(MqttCommunicationTimedOutException))]
        [TestMethod]
        public async Task TimeoutAfter()
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500)).TimeoutAfter(TimeSpan.FromMilliseconds(100));
        }

        [ExpectedException(typeof(MqttCommunicationTimedOutException))]
        [TestMethod]
        public async Task TimeoutAfterWithResult()
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500)).ContinueWith(t => 5).TimeoutAfter(TimeSpan.FromMilliseconds(100));
        }

        [TestMethod]
        public async Task TimeoutAfterCompleteInTime()
        {
            var result = await Task.Delay(TimeSpan.FromMilliseconds(100)).ContinueWith(t => 5).TimeoutAfter(TimeSpan.FromMilliseconds(500));
            Assert.AreEqual(5, result);
        }

        [TestMethod]
        public async Task TimeoutAfterWithInnerException()
        {
            try
            {
                await Task.Run(() =>
                {
                    var iis = new int[0];
                    iis[1] = 0;
                }).TimeoutAfter(TimeSpan.FromSeconds(1));

                Assert.Fail();
            }
            catch (MqttCommunicationException e)
            {
                Assert.IsTrue(e.InnerException is IndexOutOfRangeException);
            }
        }

        [TestMethod]
        public async Task TimeoutAfterWithInnerExceptionWithResult()
        {
            try
            {
                var r = await Task.Run(() =>
                {
                    var iis = new int[0];
                    return iis[1];
                }).TimeoutAfter(TimeSpan.FromSeconds(1));

                Assert.Fail();
            }
            catch (MqttCommunicationException e)
            {
                Assert.IsTrue(e.InnerException is IndexOutOfRangeException);
            }
        }

        [TestMethod]
        public async Task TimeoutAfterMemoryUsage()
        {
            var tasks = Enumerable.Range(0, 100000)
                .Select(i => Task.Delay(TimeSpan.FromMilliseconds(1)).TimeoutAfter(TimeSpan.FromMinutes(1)));

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
