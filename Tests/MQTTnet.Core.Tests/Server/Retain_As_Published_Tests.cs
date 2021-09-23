using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Formatter;
using MQTTnet.Client;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Retain_As_Published_Tests : BaseTestClass
    {
        [TestMethod]
        public Task Subscribe_With_Retain_As_Published()
        {
            return ExecuteTest(true);
        }

        [TestMethod]
        public Task Subscribe_Without_Retain_As_Published()
        {
            return ExecuteTest(false);
        }

        async Task ExecuteTest(bool retainAsPublished)
        {
            using (var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500))
            {
                await testEnvironment.StartServer();

                var client1 = await testEnvironment.ConnectClient();
                var applicationMessageHandler = testEnvironment.CreateApplicationMessageHandler(client1);
                var topicFilter = testEnvironment.Factory.CreateTopicFilterBuilder().WithTopic("Topic").WithRetainAsPublished(retainAsPublished).Build();
                await client1.SubscribeAsync(topicFilter);
                await LongTestDelay();

                // The client will publish a message where it is itself subscribing to.
                await client1.PublishAsync("Topic", "Payload", true);
                await LongTestDelay();

                applicationMessageHandler.AssertReceivedCountEquals(1);
                Assert.AreEqual(retainAsPublished, applicationMessageHandler.ReceivedApplicationMessages[0].Retain);
            }
        }
    }
}