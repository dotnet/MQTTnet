using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Serializer;

namespace MQTTnet.Core.Tests
{
    [TestClass]
    public class MqttPacketReaderTests
    {
        [TestMethod]
        public void MqttPacketReader_EmptyStream()
        {
            var memStream = new MemoryStream();
            var header = MqttPacketReader.ReadHeaderAsync(memStream, CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsNull(header);
        }
    }
}
