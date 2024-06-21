using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Formatter;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Cross_Version_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Send_V311_Receive_V500()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                await testEnvironment.StartServer();

                var receiver = await testEnvironment.ConnectClient(o => o.WithProtocolVersion(MqttProtocolVersion.V500));
                using var receivedApplicationMessages = testEnvironment.CreateApplicationMessageHandler(receiver);
                await receiver.SubscribeAsync("#");

                var sender = await testEnvironment.ConnectClient();

                var applicationMessage = new MqttApplicationMessageBuilder().WithTopic("My/Message").WithPayload("My_Payload").Build();
                await sender.PublishAsync(applicationMessage);

                await LongTestDelay();

                Assert.AreEqual(1, receivedApplicationMessages.ReceivedEventArgs.Count);
                Assert.AreEqual("My/Message", receivedApplicationMessages.ReceivedEventArgs.First().ApplicationMessage.Topic);
                Assert.AreEqual("My_Payload", receivedApplicationMessages.ReceivedEventArgs.First().ApplicationMessage.ConvertPayloadToString());
            }
        }

        [TestMethod]
        public async Task Send_V500_Receive_V311()
        {
            using (var testEnvironment = CreateTestEnvironment(MqttProtocolVersion.V500))
            {
                await testEnvironment.StartServer();

                var receiver = await testEnvironment.ConnectClient(o => o.WithProtocolVersion(MqttProtocolVersion.V311));
                using var receivedApplicationMessages = testEnvironment.CreateApplicationMessageHandler(receiver);
                await receiver.SubscribeAsync("#");
                
                var sender = await testEnvironment.ConnectClient();

                var applicationMessage = new MqttApplicationMessageBuilder().WithTopic("My/Message")
                    .WithPayload("My_Payload")
                    .WithUserProperty("A", "B")
                    .WithResponseTopic("Response")
                    .WithCorrelationData(Encoding.UTF8.GetBytes("Correlation"))
                    .Build();
                
                await sender.PublishAsync(applicationMessage);

                await LongTestDelay();

                Assert.AreEqual(1, receivedApplicationMessages.ReceivedEventArgs.Count);
                Assert.AreEqual("My/Message", receivedApplicationMessages.ReceivedEventArgs.First().ApplicationMessage.Topic);
                Assert.AreEqual("My_Payload", receivedApplicationMessages.ReceivedEventArgs.First().ApplicationMessage.ConvertPayloadToString());
            }
        }
    }
}