using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Exceptions;
using MQTTnet.Internal;
using MQTTnet.Serializer;

namespace MQTTnet.Core.Tests
{
    [TestClass]
    public class MqttPacketReaderTests
    {
        [TestMethod]
        [ExpectedException(typeof(MqttCommunicationClosedGracefullyException))]
        public void MqttPacketReader_EmptyStream()
        {
            var fixedHeader = new byte[2];
            var singleByteBuffer = new byte[1];
            MqttPacketReader.ReadFixedHeaderAsync(new TestMqttChannel(new MemoryStream()), fixedHeader, singleByteBuffer, CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
