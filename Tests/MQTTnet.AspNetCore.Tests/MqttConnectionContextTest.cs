using Microsoft.AspNetCore.Connections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.AspNetCore.Tests.Mockups;
using MQTTnet.Exceptions;
using MQTTnet.Serializer;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.AspNetCore.Tests
{
    [TestClass]
    public class MqttConnectionContextTest
    {
        [TestMethod]
        public async Task TestReceivePacketAsyncThrowsWhenReaderCompleted()
        {
            var serializer = new MqttPacketSerializer {};
            var pipe = new DuplexPipeMockup();
            var connection = new DefaultConnectionContext();
            connection.Transport = pipe;
            var ctx = new MqttConnectionContext(serializer, connection);

            pipe.Receive.Writer.Complete();

            await Assert.ThrowsExceptionAsync<MqttCommunicationException>(() => ctx.ReceivePacketAsync(TimeSpan.FromSeconds(1), CancellationToken.None));
        }
    }
}
