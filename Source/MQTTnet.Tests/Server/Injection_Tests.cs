using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet.Client;
using MQTTnet.Internal;
using MQTTnet.Server;

namespace MQTTnet.Tests.Server
{
    [TestClass]
    public sealed class Injection_Tests : BaseTestClass
    {
        [TestMethod]
        public async Task Inject_Application_Message_At_Session_Level()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();
                var receiver1 = await testEnvironment.ConnectClient();
                var receiver2 = await testEnvironment.ConnectClient();
                using var messageReceivedHandler1 = testEnvironment.CreateApplicationMessageHandler(receiver1);
                using var messageReceivedHandler2 = testEnvironment.CreateApplicationMessageHandler(receiver2);

                var status = await server.GetSessionsAsync();
                var clientStatus = status[0];

                await receiver1.SubscribeAsync("#");
                await receiver2.SubscribeAsync("#");

                await clientStatus.EnqueueApplicationMessageAsync(new MqttApplicationMessageBuilder().WithTopic("InjectedOne").Build());

                await LongTestDelay();

                Assert.AreEqual(1, messageReceivedHandler1.ReceivedEventArgs.Count);
                Assert.AreEqual("InjectedOne", messageReceivedHandler1.ReceivedEventArgs[0].ApplicationMessage.Topic);

                // The second receiver should NOT receive the message.
                Assert.AreEqual(0, messageReceivedHandler2.ReceivedEventArgs.Count);
            }
        }

        [TestMethod]
        public async Task Inject_ApplicationMessage_At_Server_Level()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();

                var receiver = await testEnvironment.ConnectClient();

                using var messageReceivedHandler = testEnvironment.CreateApplicationMessageHandler(receiver);

                await receiver.SubscribeAsync("#");

                var injectedApplicationMessage = new MqttApplicationMessageBuilder().WithTopic("InjectedOne").Build();

                await server.InjectApplicationMessage(new InjectedMqttApplicationMessage(injectedApplicationMessage));

                await LongTestDelay();

                Assert.AreEqual(1, messageReceivedHandler.ReceivedEventArgs.Count);
                Assert.AreEqual("InjectedOne", messageReceivedHandler.ReceivedEventArgs[0].ApplicationMessage.Topic);
            }
        }

        [TestMethod]
        public async Task Intercept_Injected_Application_Message()
        {
            using (var testEnvironment = CreateTestEnvironment())
            {
                var server = await testEnvironment.StartServer();

                MqttApplicationMessage interceptedMessage = null;
                server.InterceptingPublishAsync += eventArgs =>
                {
                    interceptedMessage = eventArgs.ApplicationMessage;
                    return CompletedTask.Instance;
                };

                var injectedApplicationMessage = new MqttApplicationMessageBuilder().WithTopic("InjectedOne").Build();
                await server.InjectApplicationMessage(new InjectedMqttApplicationMessage(injectedApplicationMessage));

                await LongTestDelay();

                Assert.IsNotNull(interceptedMessage);
            }
        }
    }
}