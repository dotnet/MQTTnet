using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Core.Internal;
using MQTTnet.Serializer;

namespace MQTTnet.Core.Tests
{
    [TestClass]
    public class MqttPacketReaderTests
    {
        [TestMethod]
        public void MqttPacketReader_EmptyStream()
        {
            var header = MqttPacketReader.ReadHeaderAsync(new TestMqttChannel(new MemoryStream()), CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsNull(header);
        }
    }
}
