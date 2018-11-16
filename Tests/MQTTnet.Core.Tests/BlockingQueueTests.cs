using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Internal;

namespace MQTTnet.Core.Tests
{
    [TestClass]
    public class BlockingQueueTests
    {
        [TestMethod]
        public void Preserve_Order()
        {
            var queue = new BlockingQueue<string>();
            queue.Enqueue("a");
            queue.Enqueue("b");
            queue.Enqueue("c");

            Assert.AreEqual(3, queue.Count);

            Assert.AreEqual("a", queue.Dequeue());
            Assert.AreEqual("b", queue.Dequeue());
            Assert.AreEqual("c", queue.Dequeue());
        }

        [TestMethod]
        public void Remove_First_Items()
        {
            var queue = new BlockingQueue<string>();
            queue.Enqueue("a");
            queue.Enqueue("b");
            queue.Enqueue("c");

            Assert.AreEqual("a", queue.RemoveFirst());
            Assert.AreEqual("b", queue.RemoveFirst());
            
            Assert.AreEqual(1, queue.Count);

            Assert.AreEqual("c", queue.Dequeue());
        }

        [TestMethod]
        public void Clear_Items()
        {
            var queue = new BlockingQueue<string>();
            queue.Enqueue("a");
            queue.Enqueue("b");
            queue.Enqueue("c");

            Assert.AreEqual(3, queue.Count);

            queue.Clear();

            Assert.AreEqual(0, queue.Count);
        }

        [TestMethod]
        public async Task Wait_For_Item()
        {
            var queue = new BlockingQueue<string>();

            string item = null;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(() =>
            {
                item = queue.Dequeue();
            });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            await Task.Delay(100);

            Assert.AreEqual(0, queue.Count);
            Assert.AreEqual(null, item);

            queue.Enqueue("x");

            await Task.Delay(100);

            Assert.AreEqual("x", item);
            Assert.AreEqual(0, queue.Count);
        }

        [TestMethod]
        public void Wait_For_Times()
        {
            var number = 0;

            var queue = new BlockingQueue<int>();

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(async () =>
            {
                while (true)
                {
                    queue.Enqueue(1);
                    await Task.Delay(100);
                }
            });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            while (number < 50)
            {
                queue.Dequeue();
                Interlocked.Increment(ref number);
            }
        }
    }
}
