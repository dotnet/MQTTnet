using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Internal;

namespace MQTTnet.Tests
{
    [TestClass]
    public class AsyncQueue_Tests
    {
        [TestMethod]
        public async Task Preserve_Order()
        {
            var queue = new AsyncQueue<string>();
            queue.Enqueue("1");
            queue.Enqueue("2");
            queue.Enqueue("3");

            Assert.AreEqual("1", await queue.DequeueAsync(CancellationToken.None));
            Assert.AreEqual("2", await queue.DequeueAsync(CancellationToken.None));
            Assert.AreEqual("3", await queue.DequeueAsync(CancellationToken.None));
        }

        [TestMethod]
        public async Task Preserve_ProcessAsync()
        {
            var queue = new AsyncQueue<int>();

            var sum = 0;
            var worker = Task.Run(async () => 
            {
                while (sum < 6)
                {
                    sum += await queue.DequeueAsync(CancellationToken.None);
                }
            });
            
            queue.Enqueue(1);
            await Task.Delay(500);

            queue.Enqueue(2);
            await Task.Delay(500);

            queue.Enqueue(3);
            await Task.Delay(500);

            Assert.AreEqual(6, sum);
            Assert.AreEqual(TaskStatus.RanToCompletion, worker.Status);
        }
    }
}
