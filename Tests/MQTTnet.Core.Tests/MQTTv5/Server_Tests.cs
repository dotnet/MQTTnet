using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Formatter;
using MQTTnet.Tests.Mockups;

namespace MQTTnet.Tests.MQTTv5
{
    [TestClass]
    public class Server_Tests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task Will_Message_Send()
        {
            using (var testEnvironment = new TestEnvironment(TestContext))
            {
                var receivedMessagesCount = 0;

                await testEnvironment.StartServerAsync();

                var willMessage = new MqttApplicationMessageBuilder().WithTopic("My/last/will").WithAtMostOnceQoS().Build();

                var clientOptions = new MqttClientOptionsBuilder().WithWillMessage(willMessage).WithProtocolVersion(MqttProtocolVersion.V500);

                var c1 = await testEnvironment.ConnectClientAsync(new MqttClientOptionsBuilder().WithProtocolVersion(MqttProtocolVersion.V500));
                c1.UseApplicationMessageReceivedHandler(c => Interlocked.Increment(ref receivedMessagesCount));
                await c1.SubscribeAsync(new TopicFilterBuilder().WithTopic("#").Build());

                var c2 = await testEnvironment.ConnectClientAsync(clientOptions);
                c2.Dispose(); // Dispose will not send a DISCONNECT pattern first so the will message must be sent.

                await Task.Delay(1000);

                Assert.AreEqual(1, receivedMessagesCount);
            }
        }
    }
}
