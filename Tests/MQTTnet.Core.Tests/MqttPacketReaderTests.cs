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
        [ExpectedException(typeof(MqttCommunicationException))]
        public void MqttPacketReader_EmptyStream()
        {
            MqttPacketReader.ReadFixedHeaderAsync(new TestMqttChannel(new MemoryStream()), CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
