using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Exceptions;
using MQTTnet.Formatter;
using MQTTnet.Internal;

namespace MQTTnet.Tests
{
    [TestClass]
    public class MqttPacketReaderTests
    {
        [TestMethod]
        [ExpectedException(typeof(MqttCommunicationClosedGracefullyException))]
        public void MqttPacketReader_EmptyStream()
        {
            var fixedHeader = new byte[2];
            var reader = new MqttPacketReader(new TestMqttChannel(new MemoryStream()));
            reader.ReadFixedHeaderAsync(fixedHeader, CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
