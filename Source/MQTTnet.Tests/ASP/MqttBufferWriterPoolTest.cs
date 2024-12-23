using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.AspNetCore;
using MQTTnet.Formatter;
using MQTTnet.Server;
using System;
using System.Threading.Tasks;

namespace MQTTnet.Tests.ASP
{
    [TestClass]
    public class MqttBufferWriterPoolTest
    {
        [TestMethod]
        public async Task RentReturnTest()
        {
            var services = new ServiceCollection();
            services.AddMqttServer().ConfigureMqttBufferWriterPool(p =>
            {
                p.MaxLifetime = TimeSpan.FromSeconds(1d);
            });

            var s = services.BuildServiceProvider();
            var pool = s.GetRequiredService<MqttBufferWriterPool>();
            var options = s.GetRequiredService<MqttServerOptions>();

            var bufferWriter = pool.Rent();
            Assert.AreEqual(0, pool.Count);

            Assert.IsTrue(pool.Return(bufferWriter));
            Assert.AreEqual(1, pool.Count);

            bufferWriter = pool.Rent();
            Assert.AreEqual(0, pool.Count);

            await Task.Delay(TimeSpan.FromSeconds(2d));

            Assert.IsFalse(pool.Return(bufferWriter));
            Assert.AreEqual(0, pool.Count);

            MqttBufferWriter writer = bufferWriter;
            writer.Seek(options.WriterBufferSize + 1);
            Assert.IsTrue(bufferWriter.BufferSize > options.WriterBufferSize);

            Assert.IsTrue(pool.Return(bufferWriter));
            Assert.AreEqual(1, pool.Count);
        }
    }
}
