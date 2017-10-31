using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Core.Serializer;

namespace MQTTnet.Core.Tests
{
    [TestClass]
    public class MqttPacketReaderTests
    {
        [TestMethod]
        public void MqttPacketReader_EmptyStream()
        {
            var memStream = new MemoryStream();
            var header = MqttPacketReader.ReadHeaderFromSource(memStream, CancellationToken.None);

            Assert.IsNull(header);
        }
    }
}
