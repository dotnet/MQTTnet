using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Core.Internal;
using MQTTnet.Exceptions;
using MQTTnet.Serializer;

namespace MQTTnet.Core.Tests
{
    [TestClass]
    public class MqttPacketReaderTests
    {
        [TestMethod]
        [ExpectedException(typeof(MqttCommunicationException))]
        public void MqttPacketReader_EmptyStream()
        {
            MqttPacketReader.ReadFixedHeaderAsync(new TestMqttChannel(new MemoryStream()), CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
