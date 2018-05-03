using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Internal;

namespace MQTTnet.Core.Tests
{
    [TestClass]
    public class AsyncAutoResetEventTests
    {
        [TestMethod]
        public async Task AsyncAutoResetEvent()
        {
            var aare = new AsyncAutoResetEvent();

            var increment = 0;
            var globalI = 0;
#pragma warning disable 4014
            Task.Run(async () =>
#pragma warning restore 4014
            {
                await aare.WaitOneAsync(CancellationToken.None);
                globalI += increment;
            });

            await Task.Delay(500);
            increment = 1;
            aare.Set();
            await Task.Delay(100);

            Assert.AreEqual(1, globalI);
        }
    }
}
